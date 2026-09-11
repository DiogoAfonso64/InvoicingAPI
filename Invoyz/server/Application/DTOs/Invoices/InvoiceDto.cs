namespace Invoyz.Api.Application.DTOs.Invoices;
using Invoyz.Api.Application.DTOs.InvoiceLines;

public sealed record InvoiceDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public DateOnly? IssueDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal SubTotal { get; init; }
    public decimal TotalTax { get; init; }
    public decimal GrandTotal { get; init; }
    public IReadOnlyList<InvoiceLineDto> Lines { get; init; } = Array.Empty<InvoiceLineDto>();
}
