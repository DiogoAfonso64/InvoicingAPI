using Invoyz.Api.Domain.Products;

namespace Invoyz.Api.UnitTests.Domain.Products;

public class ProductTests
{
    [Fact]
    public void Id_WhenNotSet_DefaultsToNonEmptyGuid()
    {
        var product = new Product();

        Assert.NotEqual(Guid.Empty, product.Id);
    }

    [Fact]
    public void Id_WhenCreatedTwice_ProducesDifferentValues()
    {
        var first = new Product();
        var second = new Product();

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Name_WhenNotSet_DefaultsToEmptyString()
    {
        var product = new Product();

        Assert.Equal(string.Empty, product.Name);
    }

    [Fact]
    public void Description_WhenNotSet_DefaultsToNull()
    {
        var product = new Product();

        Assert.Null(product.Description);
    }

    [Fact]
    public void UnitPrice_WhenNotSet_DefaultsToZero()
    {
        var product = new Product();

        Assert.Equal(0m, product.UnitPrice);
    }

    [Fact]
    public void TaxRate_WhenNotSet_DefaultsToZero()
    {
        var product = new Product();

        Assert.Equal(0m, product.TaxRate);
    }
}
