using Invoyz.Api.Domain.PdfJobs;

namespace Invoyz.Api.Application.PdfJobs;

public interface IPdfJobRepository
{
    Task<PdfJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<PdfJob?> GetLatestByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task AddAsync(PdfJob job, CancellationToken cancellationToken = default);
    Task UpdateAsync(PdfJob job, CancellationToken cancellationToken = default);
}
