using Invoyz.Api.Application.DTOs.Products;
using Invoyz.Api.Application.Products;
using Invoyz.Api.Controllers;
using Invoyz.Api.Domain.Common;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Invoyz.Api.UnitTests.Controllers.Products;

public class ProductControllerTests
{
    private readonly Mock<IProductService> _service = new();
    private readonly ProductController _controller;

    public ProductControllerTests()
    {
        _controller = new ProductController(_service.Object);
    }

    private static ProductDto SampleDto(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "Consulting Hour",
        Description = "One hour of consulting services",
        UnitPrice = 75m,
        TaxRate = 23m,
    };

    // ---- GetAll ----

    [Fact]
    public async Task GetAll_ReturnsOkWithProductsFromService()
    {
        var products = new List<ProductDto> { SampleDto(), SampleDto() };
        _service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(products, okResult.Value);
    }

    [Fact]
    public async Task GetAll_WhenServiceReturnsNoProducts_ReturnsOkWithEmptyList()
    {
        _service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ProductDto>());

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<ProductDto>>(okResult.Value));
    }

    // ---- GetById ----

    [Fact]
    public async Task GetById_WhenProductExists_ReturnsOkWithProduct()
    {
        var dto = SampleDto();
        _service.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, okResult.Value);
    }

    [Fact]
    public async Task GetById_WhenProductDoesNotExist_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((ProductDto?)null);

        var result = await _controller.GetById(id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Create ----

    [Fact]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedAtActionPointingToGetById()
    {
        var dto = SampleDto();
        var created = SampleDto(dto.Id);
        _service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var result = await _controller.Create(dto, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(ProductController.GetById), createdResult.ActionName);
        Assert.Equal(created.Id, createdResult.RouteValues!["id"]);
        Assert.Same(created, createdResult.Value);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsValidationException_ReturnsBadRequestWithErrors()
    {
        var dto = SampleDto();
        var errors = new List<string> { "Name is required." };
        _service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainValidationException(errors));

        var result = await _controller.Create(dto, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Same(errors, badRequest.Value);
    }

    // ---- Update ----

    [Fact]
    public async Task Update_WhenServiceSucceeds_ReturnsOkWithUpdatedProduct()
    {
        var id = Guid.NewGuid();
        var dto = SampleDto(id);
        _service.Setup(s => s.UpdateAsync(id, dto, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await _controller.Update(id, dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, okResult.Value);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsValidationException_ReturnsBadRequestWithErrors()
    {
        var id = Guid.NewGuid();
        var dto = SampleDto(id);
        var errors = new List<string> { "TaxRate must be between 0 and 100." };
        _service.Setup(s => s.UpdateAsync(id, dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainValidationException(errors));

        var result = await _controller.Update(id, dto, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Same(errors, badRequest.Value);
    }

    // ---- Patch ----

    [Fact]
    public async Task Patch_WhenProductDoesNotExist_ReturnsNotFoundWithoutCallingUpdate()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((ProductDto?)null);
        var patch = new JsonPatchDocument<ProductDto>();

        var result = await _controller.Patch(id, patch, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        _service.Verify(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ProductDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Patch_WhenOperationIsValid_AppliesItAndReturnsOkWithUpdatedProduct()
    {
        var id = Guid.NewGuid();
        var existing = SampleDto(id);
        var updated = existing with { Name = "Updated Name" };
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _service
            .Setup(s => s.UpdateAsync(id, It.Is<ProductDto>(d => d.Name == "Updated Name"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var patch = new JsonPatchDocument<ProductDto>();
        patch.Replace(x => x.Name, "Updated Name");

        var result = await _controller.Patch(id, patch, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(updated, okResult.Value);
    }

    [Fact]
    public async Task Patch_WhenOperationFailsToApply_ReturnsObjectResultWithoutCallingUpdate()
    {
        var id = Guid.NewGuid();
        var existing = SampleDto(id);
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        // A "test" operation that doesn't match the current value fails to apply,
        // which is how JsonPatchDocument reports a malformed/inconsistent patch.
        // (The exact HTTP status this produces depends on ASP.NET Core's
        // ProblemDetailsFactory, which isn't wired up outside a real request - that
        // wire-level behavior is covered by the end-to-end suite instead.)
        var patch = new JsonPatchDocument<ProductDto>();
        patch.Test(x => x.Name, "this-does-not-match-the-current-name");

        var result = await _controller.Patch(id, patch, CancellationToken.None);

        Assert.IsAssignableFrom<ObjectResult>(result.Result);
        Assert.IsNotType<OkObjectResult>(result.Result);
        _service.Verify(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ProductDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Patch_WhenResultFailsDomainValidation_ReturnsBadRequestWithErrors()
    {
        var id = Guid.NewGuid();
        var existing = SampleDto(id);
        var errors = new List<string> { "UnitPrice must be greater than or equal to 0." };
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _service
            .Setup(s => s.UpdateAsync(id, It.IsAny<ProductDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainValidationException(errors));

        var patch = new JsonPatchDocument<ProductDto>();
        patch.Replace(x => x.UnitPrice, -5m);

        var result = await _controller.Patch(id, patch, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Same(errors, badRequest.Value);
    }

    // ---- Delete ----

    [Fact]
    public async Task Delete_CallsServiceAndReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.Delete(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _service.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
