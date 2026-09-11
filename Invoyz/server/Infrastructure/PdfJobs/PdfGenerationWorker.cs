using Invoyz.Api.Application.Customers;
using Invoyz.Api.Application.DTOs.Customers;
using Invoyz.Api.Application.DTOs.Invoices;
using Invoyz.Api.Application.Invoices;
using Invoyz.Api.Application.PdfJobs;
using Invoyz.Api.Application.Products;
using Invoyz.Api.Domain.PdfJobs;
using Microsoft.Extensions.Hosting;
using QuestPDF.Fluent;

namespace Invoyz.Api.Infrastructure.PdfJobs;

// Runs for the app's whole lifetime as a singleton, dequeuing one jobId at a time
// and rendering it. Scoped Application services (InvoiceService, CustomerService,
// IProductService, IPdfJobRepository) are resolved through a fresh DI scope per
// job, since a singleton can't hold scoped dependencies directly.
public sealed class PdfGenerationWorker : BackgroundService
{
    private readonly PdfGenerationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PdfGenerationWorker> _logger;

    public PdfGenerationWorker(PdfGenerationQueue queue, IServiceScopeFactory scopeFactory, ILogger<PdfGenerationWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(jobId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unhandled error while processing PDF job {JobId}", jobId);
            }
        }
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IPdfJobRepository>();

        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
            return;

        try
        {
            job.Status = PdfJobStatus.Printing;
            await jobRepository.UpdateAsync(job, cancellationToken);

            // Snapshot everything the render needs, once, right here - so edits made
            // to the invoice/customer/products after this point can never change
            // what ends up in the PDF.
            var invoiceService = scope.ServiceProvider.GetRequiredService<InvoiceService>();
            var customerService = scope.ServiceProvider.GetRequiredService<CustomerService>();
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

            var invoice = await invoiceService.GetByIdAsync(job.InvoiceId, cancellationToken)
                ?? throw new InvalidOperationException($"Invoice {job.InvoiceId} no longer exists.");

            var customer = await customerService.GetByIdAsync(invoice.CustomerId, cancellationToken)
                ?? throw new InvalidOperationException($"Customer {invoice.CustomerId} no longer exists.");

            var productsById = (await productService.GetAllAsync(cancellationToken))
                .ToDictionary(product => product.Id);

            var document = new InvoicePdfDocument(invoice, customer, productsById);
            var content = document.GeneratePdf();

            job.Content = content;
            job.FileName = BuildFileName(invoice, customer);
            job.DownloadUrl = $"/api/invoices/{job.InvoiceId}/pdf/download";
            job.Status = PdfJobStatus.Printed;
            await jobRepository.UpdateAsync(job, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            job.Status = PdfJobStatus.Failed;
            job.Error = ex.Message;
            await jobRepository.UpdateAsync(job, cancellationToken);
        }
    }

    // e.g. "INV-0001_ACMECORP_110926" for invoice INV-0001, customer "Acme Corp",
    // issued 2026-09-11.
    private static string BuildFileName(InvoiceDto invoice, CustomerDto customer)
    {
        var customerSegment = new string(customer.Name.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (string.IsNullOrEmpty(customerSegment))
            customerSegment = "CUSTOMER";

        var dateSegment = invoice.IssueDate?.ToString("ddMMyy") ?? "NA";

        return $"{invoice.InvoiceNumber}_{customerSegment}_{dateSegment}.pdf";
    }
}
