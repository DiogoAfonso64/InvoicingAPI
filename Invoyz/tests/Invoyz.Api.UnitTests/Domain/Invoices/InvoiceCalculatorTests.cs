using Invoyz.Api.Domain.Invoices;
using Invoyz.Api.Domain.InvoiceLines;

namespace Invoyz.Api.UnitTests.Domain.Invoices;

public class InvoiceCalculatorTests
{
    // ---- CalculateLine ----

    [Fact]
    public void CalculateLine_ComputesTotalAndTaxForAStandardLine()
    {
        var (lineTotal, lineTax) = InvoiceCalculator.CalculateLine(1, 75m, 23m);

        Assert.Equal(75m, lineTotal);
        Assert.Equal(17.25m, lineTax);
    }

    [Fact]
    public void CalculateLine_ComputesCorrectlyForQuantitiesGreaterThanOne()
    {
        var (lineTotal, lineTax) = InvoiceCalculator.CalculateLine(4, 250m, 23m);

        Assert.Equal(1000m, lineTotal);
        Assert.Equal(230m, lineTax);
    }

    [Fact]
    public void CalculateLine_AppliesZeroTaxWhenTaxRateIsZero()
    {
        var (lineTotal, lineTax) = InvoiceCalculator.CalculateLine(2, 50m, 0m);

        Assert.Equal(100m, lineTotal);
        Assert.Equal(0m, lineTax);
    }

    [Fact]
    public void CalculateLine_ReturnsZeroForZeroUnitPrice()
    {
        var (lineTotal, lineTax) = InvoiceCalculator.CalculateLine(5, 0m, 23m);

        Assert.Equal(0m, lineTotal);
        Assert.Equal(0m, lineTax);
    }

    [Fact]
    public void CalculateLine_RoundsFractionalUnitPricesToTwoDecimals()
    {
        // 3 x 33.333 = 99.999, which rounds to 100.00
        var (lineTotal, _) = InvoiceCalculator.CalculateLine(3, 33.333m, 0m);

        Assert.Equal(100m, lineTotal);
    }

    [Fact]
    public void CalculateLine_StaysExactForLargeQuantitiesAtScale()
    {
        var (lineTotal, lineTax) = InvoiceCalculator.CalculateLine(1_000_000, 0.01m, 23m);

        Assert.Equal(10000m, lineTotal);
        Assert.Equal(2300m, lineTax);
    }

    [Theory]
    [InlineData(1, 0.5, 21, 0.5, 0.11)] // 0.5 * 21 / 100 = 0.105 exactly, rounds away from zero to 0.11
    [InlineData(1, 1, 23.5, 1, 0.24)] // 1 * 23.5 / 100 = 0.235 exactly, rounds away from zero to 0.24
    public void CalculateLine_RoundsExactDecimalMidpointsAwayFromZero(int quantity, decimal unitPrice, decimal taxRate, decimal expectedTotal, decimal expectedTax)
    {
        // Unlike a JS `number` (binary floating point), C# `decimal` stores 0.105
        // and 0.235 EXACTLY - there's no representation error to worry about, so
        // these are genuine, unambiguous midpoints. This is the same input pair
        // that a naive Number.EPSILON-based rounding fix on the frontend got WRONG
        // (see client/src/utils/invoiceTotals.js) - decimal arithmetic sidesteps
        // that entire bug class by construction.
        var (lineTotal, lineTax) = InvoiceCalculator.CalculateLine(quantity, unitPrice, taxRate);

        Assert.Equal(expectedTotal, lineTotal);
        Assert.Equal(expectedTax, lineTax);
    }

    [Fact]
    public void CalculateLine_ComputesTaxFromTheAlreadyRoundedLineTotal()
    {
        // Documents the chosen order of operations: tax is computed on the
        // rounded line total, not the raw unrounded quantity * unitPrice product.
        var (lineTotal, lineTax) = InvoiceCalculator.CalculateLine(1, 0.125m, 10m);

        var expectedLineTotal = Math.Round(0.125m, 2, MidpointRounding.AwayFromZero);
        var expectedTax = Math.Round(expectedLineTotal * 10m / 100m, 2, MidpointRounding.AwayFromZero);

        Assert.Equal(expectedLineTotal, lineTotal);
        Assert.Equal(expectedTax, lineTax);
    }

    // ---- CalculateInvoice ----

    [Fact]
    public void CalculateInvoice_ReturnsZeroesForNoLines()
    {
        var (subTotal, totalTax, grandTotal) = InvoiceCalculator.CalculateInvoice(Array.Empty<InvoiceLine>());

        Assert.Equal(0m, subTotal);
        Assert.Equal(0m, totalTax);
        Assert.Equal(0m, grandTotal);
    }

    [Fact]
    public void CalculateInvoice_MatchesASingleLineExactly()
    {
        var line = new InvoiceLine { LineTotal = 175m, LineTax = 40.25m };

        var (subTotal, totalTax, grandTotal) = InvoiceCalculator.CalculateInvoice(new[] { line });

        Assert.Equal(175m, subTotal);
        Assert.Equal(40.25m, totalTax);
        Assert.Equal(215.25m, grandTotal);
    }

    [Fact]
    public void CalculateInvoice_SumsTheSeededFiveLineInvoiceCorrectly()
    {
        var lines = new[]
        {
            LineFrom(1, 75m, 23m),
            LineFrom(2, 250m, 23m),
            LineFrom(3, 75m, 23m),
            LineFrom(4, 250m, 23m),
            LineFrom(5, 75m, 23m),
        };

        var (subTotal, totalTax, grandTotal) = InvoiceCalculator.CalculateInvoice(lines);

        Assert.Equal(2175m, subTotal);
        Assert.Equal(500.25m, totalTax);
        Assert.Equal(2675.25m, grandTotal);
    }

    [Fact]
    public void CalculateInvoice_AlwaysSatisfiesGrandTotalEqualsSubTotalPlusTotalTax()
    {
        var lines = new[]
        {
            LineFrom(3, 19.99m, 23m),
            LineFrom(7, 4.5m, 6m),
            LineFrom(1, 999.99m, 21m),
        };

        var (subTotal, totalTax, grandTotal) = InvoiceCalculator.CalculateInvoice(lines);

        Assert.Equal(Math.Round(subTotal + totalTax, 2, MidpointRounding.AwayFromZero), grandTotal);
    }

    [Fact]
    public void CalculateInvoice_DoesNotDriftForALargeNumberOfLines()
    {
        var lines = Enumerable.Range(0, 5000)
            .Select(i => LineFrom((i % 9) + 1, 4.49m, 13m))
            .ToList();

        var (subTotal, totalTax, grandTotal) = InvoiceCalculator.CalculateInvoice(lines);

        var expectedSubTotal = lines.Sum(l => l.LineTotal);
        var expectedTotalTax = lines.Sum(l => l.LineTax);

        Assert.Equal(expectedSubTotal, subTotal);
        Assert.Equal(expectedTotalTax, totalTax);
        Assert.Equal(Math.Round(expectedSubTotal + expectedTotalTax, 2, MidpointRounding.AwayFromZero), grandTotal);
    }

    private static InvoiceLine LineFrom(int quantity, decimal unitPrice, decimal taxRate)
    {
        var (lineTotal, lineTax) = InvoiceCalculator.CalculateLine(quantity, unitPrice, taxRate);
        return new InvoiceLine { Quantity = quantity, UnitPrice = unitPrice, TaxRate = taxRate, LineTotal = lineTotal, LineTax = lineTax };
    }
}
