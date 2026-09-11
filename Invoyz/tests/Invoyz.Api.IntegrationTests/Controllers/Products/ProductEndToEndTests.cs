using System.Net;
using System.Net.Http.Json;
using System.Text;
using Invoyz.Api.Application.DTOs.Products;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Invoyz.Api.IntegrationTests.Controllers.Products;

// These tests run the real HTTP pipeline: routing -> ProductController -> ProductService
// -> ProductValidator -> the cache-backed ProductRepository. Nothing is mocked.
// The app seeds some data on startup and the WebApplicationFactory is shared across the
// tests in this class, so every test creates and asserts on its own uniquely-named
// product rather than assuming anything about total counts.
public class ProductEndToEndTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductEndToEndTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private static ProductDto SampleDto(string? name = null) => new()
    {
        Name = name ?? $"E2E Product {Guid.NewGuid():N}",
        Description = "Created by an end-to-end test",
        UnitPrice = 42.5m,
        TaxRate = 15m,
    };

    private async Task<ProductDto> CreateProductAsync(ProductDto? dto = null)
    {
        var response = await _client.PostAsJsonAsync("/api/products", dto ?? SampleDto());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    // ---- GET /api/products ----

    [Fact]
    public async Task GetAll_ReturnsOkAndIncludesACreatedProduct()
    {
        var created = await CreateProductAsync();

        var response = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.Contains(products!, p => p.Id == created.Id);
    }

    // ---- GET /api/products/{id} ----

    [Fact]
    public async Task GetById_WhenProductExists_ReturnsOkWithMatchingProduct()
    {
        var created = await CreateProductAsync();

        var response = await _client.GetAsync($"/api/products/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(created.Name, fetched.Name);
        Assert.Equal(created.UnitPrice, fetched.UnitPrice);
    }

    [Fact]
    public async Task GetById_WhenProductDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- POST /api/products ----

    [Fact]
    public async Task Create_WithValidProduct_ReturnsCreatedWithLocationHeaderAndPersists()
    {
        var dto = SampleDto();

        var response = await _client.PostAsJsonAsync("/api/products", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotEqual(Guid.Empty, created!.Id);
        Assert.Equal(dto.Name, created.Name);

        var fetched = await _client.GetFromJsonAsync<ProductDto>(response.Headers.Location!.ToString());
        Assert.Equal(created.Id, fetched!.Id);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequestAndDoesNotPersist()
    {
        var dto = SampleDto(name: "");

        var response = await _client.PostAsJsonAsync("/api/products", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.Contains("Name is required.", errors!);
    }

    [Fact]
    public async Task Create_WithNegativeUnitPrice_ReturnsBadRequestWithErrors()
    {
        var dto = SampleDto() with { UnitPrice = -1m };

        var response = await _client.PostAsJsonAsync("/api/products", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.Contains("UnitPrice must be greater than or equal to 0.", errors!);
    }

    // ---- PUT /api/products/{id} ----

    [Fact]
    public async Task Update_WithValidProduct_PersistsChanges()
    {
        var created = await CreateProductAsync();
        var updatedDto = created with { Name = "Updated via PUT", UnitPrice = 99.99m };

        var response = await _client.PutAsJsonAsync($"/api/products/{created.Id}", updatedDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var refetched = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{created.Id}");
        Assert.Equal("Updated via PUT", refetched!.Name);
        Assert.Equal(99.99m, refetched.UnitPrice);
    }

    [Fact]
    public async Task Update_WithInvalidTaxRate_ReturnsBadRequestAndLeavesExistingDataUnchanged()
    {
        var created = await CreateProductAsync();
        var invalidDto = created with { TaxRate = 150m };

        var response = await _client.PutAsJsonAsync($"/api/products/{created.Id}", invalidDto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var refetched = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{created.Id}");
        Assert.Equal(created.TaxRate, refetched!.TaxRate);
    }

    // ---- PATCH /api/products/{id} ----

    [Fact]
    public async Task Patch_WithValidOperation_UpdatesOnlyTheTargetedField()
    {
        var created = await CreateProductAsync();
        var response = await SendPatchAsync(created.Id, "[{\"op\":\"replace\",\"path\":\"/name\",\"value\":\"Patched Name\"}]");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var patched = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("Patched Name", patched!.Name);
        Assert.Equal(created.UnitPrice, patched.UnitPrice);
    }

    [Fact]
    public async Task Patch_WhenProductDoesNotExist_ReturnsNotFound()
    {
        var response = await SendPatchAsync(Guid.NewGuid(), "[{\"op\":\"replace\",\"path\":\"/name\",\"value\":\"Ghost\"}]");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_WithResultingInvalidValue_ReturnsBadRequestAndLeavesExistingDataUnchanged()
    {
        var created = await CreateProductAsync();

        var response = await SendPatchAsync(created.Id, "[{\"op\":\"replace\",\"path\":\"/taxRate\",\"value\":250}]");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var refetched = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{created.Id}");
        Assert.Equal(created.TaxRate, refetched!.TaxRate);
    }

    private Task<HttpResponseMessage> SendPatchAsync(Guid id, string patchJson)
    {
        var content = new StringContent(patchJson, Encoding.UTF8, "application/json-patch+json");
        return _client.PatchAsync($"/api/products/{id}", content);
    }

    // ---- DELETE /api/products/{id} ----

    [Fact]
    public async Task Delete_WhenProductExists_RemovesItPermanently()
    {
        var created = await CreateProductAsync();

        var deleteResponse = await _client.DeleteAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenProductDoesNotExist_StillReturnsNoContent()
    {
        var response = await _client.DeleteAsync($"/api/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
