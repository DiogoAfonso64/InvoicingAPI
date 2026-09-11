namespace Invoyz.Api.Domain.InvoiceLines;

public sealed class InvoiceLine
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }
    public decimal LineTax { get; set; }
}
