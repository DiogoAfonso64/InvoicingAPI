using Invoyz.Api.Application.DTOs.PdfJobs;
using Invoyz.Api.Application.PdfJobs;
using Microsoft.AspNetCore.Mvc;

namespace Invoyz.Api.Controllers;

[ApiController]
[Route("api/invoices/{invoiceId:guid}/pdf")]
public sealed class InvoicePdfController : ControllerBase
{
    private readonly IInvoicePdfService _service;

    public InvoicePdfController(IInvoicePdfService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<PdfJobDto>> Start(Guid invoiceId, CancellationToken cancellationToken)
    {
        var job = await _service.StartAsync(invoiceId, cancellationToken);
        return job is null ? NotFound() : Accepted(job);
    }

    [HttpGet]
    public async Task<ActionResult<PdfJobDto>> GetStatus(Guid invoiceId, CancellationToken cancellationToken)
    {
        var job = await _service.GetStatusAsync(invoiceId, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download(Guid invoiceId, CancellationToken cancellationToken)
    {
        var download = await _service.DownloadAsync(invoiceId, cancellationToken);
        return download is null ? NotFound() : File(download.Content, "application/pdf", download.FileName);
    }
}
