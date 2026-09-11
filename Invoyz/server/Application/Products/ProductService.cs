using Invoyz.Api.Application.DTOs.Products;
using Invoyz.Api.Domain.Common;
using Invoyz.Api.Domain.Products;

namespace Invoyz.Api.Application.Products;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IValidator<Product> _validator;

    public ProductService(IProductRepository repository, IValidator<Product> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _repository.GetAllAsync(cancellationToken);
        return products.Select(ToDto).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken);
        return product is null ? null : ToDto(product);
    }

    public async Task<ProductDto> CreateAsync(ProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = ToEntity(dto, Guid.NewGuid());
        Validate(product);
        await _repository.AddAsync(product, cancellationToken);
        return ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, ProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = ToEntity(dto, id);
        Validate(product);
        await _repository.UpdateAsync(product, cancellationToken);
        return ToDto(product);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    private void Validate(Product product)
    {
        var result = _validator.Validate(product);
        if (!result.IsValid)
            throw new DomainValidationException(result.Errors);
    }

    private static ProductDto ToDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        UnitPrice = product.UnitPrice,
        TaxRate = product.TaxRate
    };

    private static Product ToEntity(ProductDto dto, Guid id) => new()
    {
        Id = id,
        Name = dto.Name,
        Description = dto.Description,
        UnitPrice = dto.UnitPrice,
        TaxRate = dto.TaxRate
    };
}
