using Invoyz.Api.Domain.InvoiceLines;

namespace Invoyz.Api.Domain.Invoices;

// The single source of truth for invoice/line totals and tax. Computed here,
// server-side, rather than trusted from the client: decimal is exact base-10
// arithmetic (unlike a JS number, which is binary floating point), so this has
// none of the representation-error rounding pitfalls that plain multiplication
// and division can hit in JavaScript.
public static class InvoiceCalculator
{
    public static (decimal LineTotal, decimal LineTax) CalculateLine(int quantity, decimal unitPrice, decimal taxRate)
    {
        var lineTotal = Round(quantity * unitPrice);
        var lineTax = Round(lineTotal * taxRate / 100m);
        return (lineTotal, lineTax);
    }

    public static (decimal SubTotal, decimal TotalTax, decimal GrandTotal) CalculateInvoice(IEnumerable<InvoiceLine> lines)
    {
        var subTotal = Round(lines.Sum(line => line.LineTotal));
        var totalTax = Round(lines.Sum(line => line.LineTax));
        return (subTotal, totalTax, Round(subTotal + totalTax));
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
