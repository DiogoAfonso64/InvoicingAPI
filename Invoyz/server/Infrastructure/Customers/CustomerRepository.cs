using Invoyz.Api.Application.Customers;
using Invoyz.Api.Domain.Customers;
using Invoyz.Api.Infrastructure.Common;
using Microsoft.Extensions.Caching.Memory;

namespace Invoyz.Api.Infrastructure.Customers;

public sealed class CustomerRepository : CacheRepositoryBase<Customer>, ICustomerRepository
{
    public CustomerRepository(IMemoryCache cache) : base(cache, "customer")
    {
    }

    public Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
        => GetAllEntriesAsync(cancellationToken);

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => GetEntryAsync(id, cancellationToken);

    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        => SetEntryAsync(customer.Id, customer, cancellationToken);

    public Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
        => SetEntryAsync(customer.Id, customer, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => RemoveEntryAsync(id, cancellationToken);
}
