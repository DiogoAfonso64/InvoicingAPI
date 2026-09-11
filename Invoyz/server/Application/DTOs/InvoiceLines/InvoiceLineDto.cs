namespace Invoyz.Api.Application.DTOs.InvoiceLines;

public sealed record InvoiceLineDto
{
    public Guid Id { get; init; }
    public Guid InvoiceId { get; init; }
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TaxRate { get; init; }
    public decimal LineTotal { get; init; }
    public decimal LineTax { get; init; }
}
