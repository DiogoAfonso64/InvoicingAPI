using Invoyz.Api.Application.DTOs.Customers;
using Invoyz.Api.Application.DTOs.Invoices;
using Invoyz.Api.Application.DTOs.Products;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Invoyz.Api.Infrastructure.PdfJobs;

public sealed class InvoicePdfDocument : IDocument
{
    private readonly InvoiceDto _invoice;
    private readonly CustomerDto _customer;
    private readonly IReadOnlyDictionary<Guid, ProductDto> _productsById;

    public InvoicePdfDocument(InvoiceDto invoice, CustomerDto customer, IReadOnlyDictionary<Guid, ProductDto> productsById)
    {
        _invoice = invoice;
        _customer = customer;
        _productsById = productsById;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().AlignCenter().Text(text =>
            {
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text($"Invoice {_invoice.InvoiceNumber}").FontSize(18).Bold();
                column.Item().Text($"Status: {_invoice.Status}");
                column.Item().Text($"Issue date: {FormatDate(_invoice.IssueDate)}");
                column.Item().Text($"Due date: {FormatDate(_invoice.DueDate)}");
            });

            row.RelativeItem().Column(column =>
            {
                column.Item().AlignRight().Text("Bill to").Bold();
                column.Item().AlignRight().Text(_customer.Name);
                column.Item().AlignRight().Text(_customer.Email);
                column.Item().AlignRight().Text(_customer.Address);
                column.Item().AlignRight().Text(_customer.VatNumber);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(12);
            column.Item().Element(ComposeLinesTable);
            column.Item().AlignRight().Element(ComposeTotals);
        });
    }

    private void ComposeLinesTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
            });

            table.Header(header =>
            {
                header.Cell().Text("Product").Bold();
                header.Cell().AlignRight().Text("Qty").Bold();
                header.Cell().AlignRight().Text("Unit price").Bold();
                header.Cell().AlignRight().Text("Tax %").Bold();
                header.Cell().AlignRight().Text("Line total").Bold();

                header.Cell().ColumnSpan(5).PaddingTop(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten1);
            });

            foreach (var line in _invoice.Lines)
            {
                var productName = _productsById.TryGetValue(line.ProductId, out var product)
                    ? product.Name
                    : "Unknown product";

                table.Cell().Text(productName);
                table.Cell().AlignRight().Text(line.Quantity.ToString());
                table.Cell().AlignRight().Text(FormatMoney(line.UnitPrice));
                table.Cell().AlignRight().Text($"{line.TaxRate}%");
                table.Cell().AlignRight().Text(FormatMoney(line.LineTotal));
            }
        });
    }

    private void ComposeTotals(IContainer container)
    {
        container.Width(220).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Subtotal");
                row.ConstantItem(90).AlignRight().Text(FormatMoney(_invoice.SubTotal));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Tax");
                row.ConstantItem(90).AlignRight().Text(FormatMoney(_invoice.TotalTax));
            });
            column.Item().PaddingTop(4).BorderTop(1).BorderColor(Colors.Grey.Lighten1).Row(row =>
            {
                row.RelativeItem().Text("Grand total").Bold();
                row.ConstantItem(90).AlignRight().Text(FormatMoney(_invoice.GrandTotal)).Bold();
            });
        });
    }

    private static string FormatDate(DateOnly? date) => date?.ToString("yyyy-MM-dd") ?? "—";

    private static string FormatMoney(decimal value) => $"{value:N2} €";
}
