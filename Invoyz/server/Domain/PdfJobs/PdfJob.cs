namespace Invoyz.Api.Domain.PdfJobs;

public sealed class PdfJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public PdfJobStatus Status { get; set; } = PdfJobStatus.Pending;
    public string? DownloadUrl { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public byte[]? Content { get; set; }
    public string? FileName { get; set; }
}
