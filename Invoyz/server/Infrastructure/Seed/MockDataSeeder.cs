using Invoyz.Api.Application.Customers;
using Invoyz.Api.Application.DTOs.Customers;
using Invoyz.Api.Application.DTOs.InvoiceLines;
using Invoyz.Api.Application.DTOs.Invoices;
using Invoyz.Api.Application.DTOs.Products;
using Invoyz.Api.Application.InvoiceLines;
using Invoyz.Api.Application.Invoices;
using Invoyz.Api.Application.Products;

namespace Invoyz.Api.Infrastructure.Seed;

public static class MockDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var customerService = services.GetRequiredService<CustomerService>();
        var productService = services.GetRequiredService<IProductService>();
        var invoiceService = services.GetRequiredService<InvoiceService>();
        var invoiceLineService = services.GetRequiredService<InvoiceLineService>();

        var customer1 = await customerService.CreateAsync(new CustomerDto
        {
            Name = "Acme Corp",
            Email = "billing@acme.test",
            Address = "1 Industrial Way, Lisbon",
            VatNumber = "PT123456789"
        }, cancellationToken);

        var customer2 = await customerService.CreateAsync(new CustomerDto
        {
            Name = "Globex Ltd",
            Email = "accounts@globex.test",
            Address = "42 Market Street, Porto",
            VatNumber = "PT987654321"
        }, cancellationToken);

        var product1 = await productService.CreateAsync(new ProductDto
        {
            Name = "Consulting Hour",
            Description = "One hour of consulting services",
            UnitPrice = 75m,
            TaxRate = 23m
        }, cancellationToken);

        var product2 = await productService.CreateAsync(new ProductDto
        {
            Name = "Software License",
            Description = "Annual software license seat",
            UnitPrice = 250m,
            TaxRate = 23m
        }, cancellationToken);

        var products = new[] { product1, product2 };

        await CreateInvoiceWithLinesAsync(invoiceService, invoiceLineService, "INV-0001", customer1.Id, lineCount: 5, products, cancellationToken);
        await CreateInvoiceWithLinesAsync(invoiceService, invoiceLineService, "INV-0002", customer2.Id, lineCount: 7, products, cancellationToken);
        await CreateInvoiceWithLinesAsync(invoiceService, invoiceLineService, "INV-0003", customer1.Id, lineCount: 3, products, cancellationToken);
    }

    private static async Task CreateInvoiceWithLinesAsync(
        InvoiceService invoiceService,
        InvoiceLineService invoiceLineService,
        string invoiceNumber,
        Guid customerId,
        int lineCount,
        IReadOnlyList<ProductDto> products,
        CancellationToken cancellationToken)
    {
        var issueDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Create the invoice first so the repository assigns its real Id (CreateAsync
        // always mints its own Guid), then attach lines built against that real Id.
        var created = await invoiceService.CreateAsync(new InvoiceDto
        {
            InvoiceNumber = invoiceNumber,
            CustomerId = customerId,
            IssueDate = issueDate,
            DueDate = issueDate.AddDays(30),
            Status = "Draft"
        }, cancellationToken);

        var lines = new List<InvoiceLineDto>();
        for (var i = 0; i < lineCount; i++)
        {
            var product = products[i % products.Count];
            var quantity = i + 1;
            var lineTotal = product.UnitPrice * quantity;
            var lineTax = Math.Round(lineTotal * product.TaxRate / 100m, 2);

            lines.Add(new InvoiceLineDto
            {
                Id = Guid.NewGuid(),
                InvoiceId = created.Id,
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.UnitPrice,
                TaxRate = product.TaxRate,
                LineTotal = lineTotal,
                LineTax = lineTax
            });
        }

        var subTotal = lines.Sum(line => line.LineTotal);
        var totalTax = lines.Sum(line => line.LineTax);

        await invoiceService.UpdateAsync(created.Id, created with
        {
            SubTotal = subTotal,
            TotalTax = totalTax,
            GrandTotal = subTotal + totalTax,
            Lines = lines
        }, cancellationToken);

        foreach (var line in lines)
            await invoiceLineService.CreateAsync(line, cancellationToken);
    }
}
