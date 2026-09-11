namespace Invoyz.Api.Application.DTOs.PdfJobs;

public sealed record PdfJobDto
{
    public Guid JobId { get; init; }
    public Guid InvoiceId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? DownloadUrl { get; init; }
    public string? Error { get; init; }
    public DateTime CreatedAt { get; init; }
}
