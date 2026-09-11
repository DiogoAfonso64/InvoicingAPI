using Invoyz.Api.Application.PdfJobs;
using Invoyz.Api.Domain.PdfJobs;
using Invoyz.Api.Infrastructure.Common;
using Microsoft.Extensions.Caching.Memory;

namespace Invoyz.Api.Infrastructure.PdfJobs;

public sealed class PdfJobRepository : CacheRepositoryBase<PdfJob>, IPdfJobRepository
{
    public PdfJobRepository(IMemoryCache cache) : base(cache, "pdfjob")
    {
    }

    public Task<PdfJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
        => GetEntryAsync(jobId, cancellationToken);

    public async Task<PdfJob?> GetLatestByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var jobs = await GetAllEntriesAsync(cancellationToken);
        return jobs
            .Where(job => job.InvoiceId == invoiceId)
            .OrderByDescending(job => job.CreatedAt)
            .FirstOrDefault();
    }

    public Task AddAsync(PdfJob job, CancellationToken cancellationToken = default)
        => SetEntryAsync(job.Id, job, cancellationToken);

    public Task UpdateAsync(PdfJob job, CancellationToken cancellationToken = default)
        => SetEntryAsync(job.Id, job, cancellationToken);
}
