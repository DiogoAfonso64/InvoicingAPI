using Invoyz.Api.Domain.InvoiceLines;

namespace Invoyz.Api.Application.InvoiceLines;

public interface IInvoiceLineRepository
{
    Task<IReadOnlyList<InvoiceLine>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<InvoiceLine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(InvoiceLine line, CancellationToken cancellationToken = default);
    Task UpdateAsync(InvoiceLine line, CancellationToken cancellationToken = default);
    Task UpdateManyAsync(IEnumerable<InvoiceLine> lines, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
