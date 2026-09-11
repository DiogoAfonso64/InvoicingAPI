using Invoyz.Api.Application.DTOs.PdfJobs;
using Invoyz.Api.Application.Invoices;
using Invoyz.Api.Domain.PdfJobs;

namespace Invoyz.Api.Application.PdfJobs;

public sealed class InvoicePdfService : IInvoicePdfService
{
    private readonly IPdfJobRepository _jobRepository;
    private readonly IPdfGenerationQueue _queue;
    private readonly InvoiceService _invoiceService;

    public InvoicePdfService(IPdfJobRepository jobRepository, IPdfGenerationQueue queue, InvoiceService invoiceService)
    {
        _jobRepository = jobRepository;
        _queue = queue;
        _invoiceService = invoiceService;
    }

    public async Task<PdfJobDto?> StartAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceService.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
            return null;

        // Don't queue a second render while one is already in flight for this invoice.
        var existing = await _jobRepository.GetLatestByInvoiceIdAsync(invoiceId, cancellationToken);
        if (existing is not null && existing.Status is PdfJobStatus.Pending or PdfJobStatus.Printing)
            return ToDto(existing);

        var job = new PdfJob
        {
            InvoiceId = invoiceId,
            Status = PdfJobStatus.Pending,
        };

        await _jobRepository.AddAsync(job, cancellationToken);
        await _queue.EnqueueAsync(job.Id, cancellationToken);

        return ToDto(job);
    }

    public async Task<PdfJobDto?> GetStatusAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetLatestByInvoiceIdAsync(invoiceId, cancellationToken);
        return job is null ? null : ToDto(job);
    }

    public async Task<PdfDownload?> DownloadAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetLatestByInvoiceIdAsync(invoiceId, cancellationToken);
        if (job is null || job.Status != PdfJobStatus.Printed || job.Content is null)
            return null;

        return new PdfDownload(new MemoryStream(job.Content), job.FileName ?? $"invoice-{invoiceId}.pdf");
    }

    private static PdfJobDto ToDto(PdfJob job) => new()
    {
        JobId = job.Id,
        InvoiceId = job.InvoiceId,
        Status = job.Status.ToString(),
        DownloadUrl = job.DownloadUrl,
        Error = job.Error,
        CreatedAt = job.CreatedAt,
    };
}
