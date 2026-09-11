using Invoyz.Api.Domain.Products;

namespace Invoyz.Api.UnitTests.Domain.Products;

public class ProductValidatorTests
{
    private readonly ProductValidator _validator = new();

    private static Product CreateProduct(
        Guid? id = null,
        string? name = null,
        string? description = "One hour of consulting services",
        decimal? unitPrice = null,
        decimal? taxRate = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = name ?? "Consulting Hour",
        Description = description,
        UnitPrice = unitPrice ?? 75m,
        TaxRate = taxRate ?? 23m,
    };

    [Fact]
    public void Validate_WithValidProduct_ReturnsSuccess()
    {
        var result = _validator.Validate(CreateProduct());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // ---- Id ----

    [Fact]
    public void Validate_WithEmptyId_ReturnsError()
    {
        var result = _validator.Validate(CreateProduct(id: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains("Id is required.", result.Errors);
    }

    // ---- Name ----

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var result = _validator.Validate(CreateProduct(name: ""));

        Assert.False(result.IsValid);
        Assert.Contains("Name is required.", result.Errors);
    }

    [Fact]
    public void Validate_WithWhitespaceOnlyName_ReturnsError()
    {
        var result = _validator.Validate(CreateProduct(name: "   "));

        Assert.False(result.IsValid);
        Assert.Contains("Name is required.", result.Errors);
    }

    [Fact]
    public void Validate_WithNameSurroundedByWhitespace_TrimsBeforeValidatingAndSucceeds()
    {
        var result = _validator.Validate(CreateProduct(name: "   Widget   "));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithNameAtMaxLength_ReturnsSuccess()
    {
        var name = new string('A', 200);

        var result = _validator.Validate(CreateProduct(name: name));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithNameExceedingMaxLength_ReturnsError()
    {
        var name = new string('A', 201);

        var result = _validator.Validate(CreateProduct(name: name));

        Assert.False(result.IsValid);
        Assert.Contains("Name must not exceed 200 characters.", result.Errors);
    }

    // ---- Description (optional) ----

    [Fact]
    public void Validate_WithNullDescription_ReturnsSuccess()
    {
        var result = _validator.Validate(CreateProduct(description: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyDescription_ReturnsSuccess()
    {
        var result = _validator.Validate(CreateProduct(description: ""));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithDescriptionAtMaxLength_ReturnsSuccess()
    {
        var description = new string('A', 1000);

        var result = _validator.Validate(CreateProduct(description: description));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithDescriptionExceedingMaxLength_ReturnsError()
    {
        var description = new string('A', 1001);

        var result = _validator.Validate(CreateProduct(description: description));

        Assert.False(result.IsValid);
        Assert.Contains("Description must not exceed 1000 characters.", result.Errors);
    }

    // ---- UnitPrice ----

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(9999999.99)]
    public void Validate_WithNonNegativeUnitPrice_ReturnsSuccess(decimal unitPrice)
    {
        var result = _validator.Validate(CreateProduct(unitPrice: unitPrice));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-9999999.99)]
    public void Validate_WithNegativeUnitPrice_ReturnsError(decimal unitPrice)
    {
        var result = _validator.Validate(CreateProduct(unitPrice: unitPrice));

        Assert.False(result.IsValid);
        Assert.Contains("UnitPrice must be greater than or equal to 0.", result.Errors);
    }

    // ---- TaxRate ----

    [Theory]
    [InlineData(0)]
    [InlineData(23.5)]
    [InlineData(100)]
    public void Validate_WithTaxRateWithinRange_ReturnsSuccess(decimal taxRate)
    {
        var result = _validator.Validate(CreateProduct(taxRate: taxRate));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(100.01)]
    [InlineData(101)]
    public void Validate_WithTaxRateOutsideRange_ReturnsError(decimal taxRate)
    {
        var result = _validator.Validate(CreateProduct(taxRate: taxRate));

        Assert.False(result.IsValid);
        Assert.Contains("TaxRate must be between 0 and 100.", result.Errors);
    }

    // ---- Multiple failures accumulate ----

    [Fact]
    public void Validate_WithMultipleInvalidFields_ReturnsAllCorrespondingErrors()
    {
        var product = CreateProduct(
            id: Guid.Empty,
            name: "",
            unitPrice: -10m,
            taxRate: 150m);

        var result = _validator.Validate(product);

        Assert.False(result.IsValid);
        Assert.Equal(4, result.Errors.Count);
        Assert.Contains("Id is required.", result.Errors);
        Assert.Contains("Name is required.", result.Errors);
        Assert.Contains("UnitPrice must be greater than or equal to 0.", result.Errors);
        Assert.Contains("TaxRate must be between 0 and 100.", result.Errors);
    }
}
