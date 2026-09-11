using Invoyz.Api.Application.DTOs.InvoiceLines;
using Invoyz.Api.Application.InvoiceLines;
using Invoyz.Api.Domain.Common;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Invoyz.Api.Controllers;

[ApiController]
[Route("api/invoicelines")]
public sealed class InvoiceLineController : ControllerBase
{
    private readonly InvoiceLineService _service;

    public InvoiceLineController(InvoiceLineService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceLineDto>>> GetByInvoiceId([FromQuery] Guid invoiceId, CancellationToken cancellationToken)
    {
        if (invoiceId == Guid.Empty)
            return BadRequest("invoiceId query parameter is required.");

        return Ok(await _service.GetByInvoiceIdAsync(invoiceId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceLineDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var line = await _service.GetByIdAsync(id, cancellationToken);
        return line is null ? NotFound() : Ok(line);
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceLineDto>> Create([FromBody] InvoiceLineDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DomainValidationException ex)
        {
            return BadRequest(ex.Errors);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InvoiceLineDto>> Update(Guid id, [FromBody] InvoiceLineDto dto, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.UpdateAsync(id, dto, cancellationToken));
        }
        catch (DomainValidationException ex)
        {
            return BadRequest(ex.Errors);
        }
    }

    [HttpPut("bulk")]
    public async Task<ActionResult<IReadOnlyList<InvoiceLineDto>>> BulkUpdate([FromBody] IReadOnlyList<InvoiceLineDto> lines, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.BulkUpdateAsync(lines, cancellationToken));
        }
        catch (DomainValidationException ex)
        {
            return BadRequest(ex.Errors);
        }
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<InvoiceLineDto>> Patch(Guid id, [FromBody] JsonPatchDocument<InvoiceLineDto> patchDocument, CancellationToken cancellationToken)
    {
        var existing = await _service.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        patchDocument.ApplyTo(existing, ModelState);
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            return Ok(await _service.UpdateAsync(id, existing, cancellationToken));
        }
        catch (DomainValidationException ex)
        {
            return BadRequest(ex.Errors);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
