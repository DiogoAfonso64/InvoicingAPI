namespace Invoyz.Api.Application.PdfJobs;

public interface IPdfGenerationQueue
{
    Task EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default);
}
