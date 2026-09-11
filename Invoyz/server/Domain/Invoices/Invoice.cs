using Invoyz.Api.Domain.InvoiceLines;

namespace Invoyz.Api.Domain.Invoices;

public sealed class Invoice
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }
    public List<InvoiceLine> Lines { get; init; } = new();
}
