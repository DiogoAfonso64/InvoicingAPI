using Invoyz.Api.Application.DTOs.PdfJobs;

namespace Invoyz.Api.Application.PdfJobs;

public interface IInvoicePdfService
{
    Task<PdfJobDto?> StartAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<PdfJobDto?> GetStatusAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<PdfDownload?> DownloadAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}
