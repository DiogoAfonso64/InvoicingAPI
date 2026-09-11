using Invoyz.Api.Application.Products;
using Invoyz.Api.Domain.Products;
using Invoyz.Api.Infrastructure.Common;
using Microsoft.Extensions.Caching.Memory;

namespace Invoyz.Api.Infrastructure.Products;

public sealed class ProductRepository : CacheRepositoryBase<Product>, IProductRepository
{
    public ProductRepository(IMemoryCache cache) : base(cache, "product")
    {
    }

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        => GetAllEntriesAsync(cancellationToken);

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => GetEntryAsync(id, cancellationToken);

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
        => SetEntryAsync(product.Id, product, cancellationToken);

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
        => SetEntryAsync(product.Id, product, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => RemoveEntryAsync(id, cancellationToken);
}
