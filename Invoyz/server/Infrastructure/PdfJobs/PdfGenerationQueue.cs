using System.Threading.Channels;
using Invoyz.Api.Application.PdfJobs;

namespace Invoyz.Api.Infrastructure.PdfJobs;

// Registered as a singleton: it's the hand-off point between per-request Scoped
// code (InvoicePdfService, via IPdfGenerationQueue) and the long-lived background
// PdfGenerationWorker, which reads the same instance directly as its concrete type.
public sealed class PdfGenerationQueue : IPdfGenerationQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public async Task EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default)
        => await _channel.Writer.WriteAsync(jobId, cancellationToken);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken = default)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
