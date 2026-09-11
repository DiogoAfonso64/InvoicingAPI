using Invoyz.Api.Application.DTOs.Products;
using Invoyz.Api.Application.Products;
using Invoyz.Api.Domain.Common;
using Invoyz.Api.Domain.Products;
using Moq;

namespace Invoyz.Api.UnitTests.Application.Products;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        // The real validator is used deliberately: it's already covered by its own
        // test suite, and composing it here verifies ProductService actually triggers
        // validation and propagates its result, not just that it calls some IValidator.
        _service = new ProductService(_repository.Object, new ProductValidator());
    }

    private static ProductDto ValidDto(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "Consulting Hour",
        Description = "One hour of consulting services",
        UnitPrice = 75m,
        TaxRate = 23m,
    };

    private static Product SampleProduct(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "Software License",
        Description = "Annual software license seat",
        UnitPrice = 250m,
        TaxRate = 23m,
    };

    // ---- GetAllAsync ----

    [Fact]
    public async Task GetAllAsync_MapsEachRepositoryProductToADto()
    {
        var products = new List<Product> { SampleProduct(), SampleProduct() };
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(products[0].Id, result[0].Id);
        Assert.Equal(products[0].Name, result[0].Name);
        Assert.Equal(products[0].UnitPrice, result[0].UnitPrice);
        Assert.Equal(products[0].TaxRate, result[0].TaxRate);
    }

    [Fact]
    public async Task GetAllAsync_WhenRepositoryIsEmpty_ReturnsEmptyList()
    {
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product>());

        var result = await _service.GetAllAsync();

        Assert.Empty(result);
    }

    // ---- GetByIdAsync ----

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsMappedDto()
    {
        var product = SampleProduct();
        _repository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _service.GetByIdAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal(product.Id, result!.Id);
        Assert.Equal(product.Description, result.Description);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var result = await _service.GetByIdAsync(id);

        Assert.Null(result);
    }

    // ---- CreateAsync ----

    [Fact]
    public async Task CreateAsync_WithValidDto_AddsProductAndReturnsMappedDto()
    {
        Product? captured = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .Returns(Task.CompletedTask);

        var dto = ValidDto();

        var result = await _service.CreateAsync(dto);

        _repository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(captured);
        Assert.Equal(dto.Name, captured!.Name);
        Assert.Equal(dto.UnitPrice, captured.UnitPrice);
        Assert.Equal(result.Id, captured.Id);
    }

    [Fact]
    public async Task CreateAsync_IgnoresIncomingId_AndAlwaysAssignsANewOne()
    {
        var suppliedId = Guid.NewGuid();
        _repository.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(ValidDto(suppliedId));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotEqual(suppliedId, result.Id);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidDto_ThrowsDomainValidationExceptionAndNeverCallsRepository()
    {
        var invalidDto = ValidDto() with { Name = "" };

        var exception = await Assert.ThrowsAsync<DomainValidationException>(() => _service.CreateAsync(invalidDto));

        Assert.Contains("Name is required.", exception.Errors);
        _repository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithMultipleInvalidFields_ThrowsWithAllCorrespondingErrors()
    {
        var invalidDto = ValidDto() with { UnitPrice = -1m, TaxRate = 200m };

        var exception = await Assert.ThrowsAsync<DomainValidationException>(() => _service.CreateAsync(invalidDto));

        Assert.Contains("UnitPrice must be greater than or equal to 0.", exception.Errors);
        Assert.Contains("TaxRate must be between 0 and 100.", exception.Errors);
        _repository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- UpdateAsync ----

    [Fact]
    public async Task UpdateAsync_UsesTheRouteId_NotTheIdOnThePayload()
    {
        var routeId = Guid.NewGuid();
        var payloadWithDifferentId = ValidDto(Guid.NewGuid());
        Product? captured = null;
        _repository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(routeId, payloadWithDifferentId);

        Assert.Equal(routeId, result.Id);
        Assert.Equal(routeId, captured!.Id);
    }

    [Fact]
    public async Task UpdateAsync_WithValidDto_CallsRepositoryOnceAndReturnsMappedDto()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(id, ValidDto());

        _repository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidDto_ThrowsDomainValidationExceptionAndNeverCallsRepository()
    {
        var invalidDto = ValidDto() with { Description = new string('A', 1001) };

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => _service.UpdateAsync(Guid.NewGuid(), invalidDto));

        Assert.Contains("Description must not exceed 1000 characters.", exception.Errors);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- DeleteAsync ----

    [Fact]
    public async Task DeleteAsync_DelegatesToRepositoryWithTheGivenId()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.DeleteAsync(id);

        _repository.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
