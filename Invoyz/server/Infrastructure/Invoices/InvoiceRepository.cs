using Invoyz.Api.Application.Invoices;
using Invoyz.Api.Domain.Invoices;
using Invoyz.Api.Infrastructure.Common;
using Microsoft.Extensions.Caching.Memory;

namespace Invoyz.Api.Infrastructure.Invoices;

public sealed class InvoiceRepository : CacheRepositoryBase<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(IMemoryCache cache) : base(cache, "invoice")
    {
    }

    public Task<IReadOnlyList<Invoice>> GetAllAsync(CancellationToken cancellationToken = default)
        => GetAllEntriesAsync(cancellationToken);

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => GetEntryAsync(id, cancellationToken);

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
        => SetEntryAsync(invoice.Id, invoice, cancellationToken);

    public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
        => SetEntryAsync(invoice.Id, invoice, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => RemoveEntryAsync(id, cancellationToken);
}
