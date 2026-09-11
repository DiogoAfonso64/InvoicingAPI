using Invoyz.Api.Application.InvoiceLines;
using Invoyz.Api.Domain.InvoiceLines;
using Invoyz.Api.Infrastructure.Common;
using Microsoft.Extensions.Caching.Memory;

namespace Invoyz.Api.Infrastructure.InvoiceLines;

public sealed class InvoiceLineRepository : CacheRepositoryBase<InvoiceLine>, IInvoiceLineRepository
{
    public InvoiceLineRepository(IMemoryCache cache) : base(cache, "invoiceline")
    {
    }

    public async Task<IReadOnlyList<InvoiceLine>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var lines = await GetAllEntriesAsync(cancellationToken);
        return lines.Where(line => line.InvoiceId == invoiceId).ToList();
    }

    public Task<InvoiceLine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => GetEntryAsync(id, cancellationToken);

    public Task AddAsync(InvoiceLine line, CancellationToken cancellationToken = default)
        => SetEntryAsync(line.Id, line, cancellationToken);

    public Task UpdateAsync(InvoiceLine line, CancellationToken cancellationToken = default)
        => SetEntryAsync(line.Id, line, cancellationToken);

    public async Task UpdateManyAsync(IEnumerable<InvoiceLine> lines, CancellationToken cancellationToken = default)
    {
        foreach (var line in lines)
            await SetEntryAsync(line.Id, line, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => RemoveEntryAsync(id, cancellationToken);
}
