using Invoyz.Api.Application.DTOs.InvoiceLines;
using Invoyz.Api.Domain.Common;
using Invoyz.Api.Domain.Invoices;
using Invoyz.Api.Domain.InvoiceLines;

namespace Invoyz.Api.Application.InvoiceLines;

public sealed class InvoiceLineService
{
    private readonly IInvoiceLineRepository _repository;
    private readonly IValidator<InvoiceLine> _validator;

    public InvoiceLineService(IInvoiceLineRepository repository, IValidator<InvoiceLine> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<IReadOnlyList<InvoiceLineDto>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var lines = await _repository.GetByInvoiceIdAsync(invoiceId, cancellationToken);
        return lines.Select(ToDto).ToList();
    }

    public async Task<InvoiceLineDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var line = await _repository.GetByIdAsync(id, cancellationToken);
        return line is null ? null : ToDto(line);
    }

    public async Task<InvoiceLineDto> CreateAsync(InvoiceLineDto dto, CancellationToken cancellationToken = default)
    {
        var line = ToEntity(dto, Guid.NewGuid());
        Validate(line);
        await _repository.AddAsync(line, cancellationToken);
        return ToDto(line);
    }

    public async Task<InvoiceLineDto> UpdateAsync(Guid id, InvoiceLineDto dto, CancellationToken cancellationToken = default)
    {
        var line = ToEntity(dto, id);
        Validate(line);
        await _repository.UpdateAsync(line, cancellationToken);
        return ToDto(line);
    }

    public async Task<IReadOnlyList<InvoiceLineDto>> BulkUpdateAsync(IEnumerable<InvoiceLineDto> dtos, CancellationToken cancellationToken = default)
    {
        var lines = dtos.Select(dto => ToEntity(dto, dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id)).ToList();
        foreach (var line in lines)
            Validate(line);

        await _repository.UpdateManyAsync(lines, cancellationToken);
        return lines.Select(ToDto).ToList();
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    private void Validate(InvoiceLine line)
    {
        var result = _validator.Validate(line);
        if (!result.IsValid)
            throw new DomainValidationException(result.Errors);
    }

    private static InvoiceLineDto ToDto(InvoiceLine line) => new()
    {
        Id = line.Id,
        InvoiceId = line.InvoiceId,
        ProductId = line.ProductId,
        Quantity = line.Quantity,
        UnitPrice = line.UnitPrice,
        TaxRate = line.TaxRate,
        LineTotal = line.LineTotal,
        LineTax = line.LineTax
    };

    private static InvoiceLine ToEntity(InvoiceLineDto dto, Guid id)
    {
        var (lineTotal, lineTax) = InvoiceCalculator.CalculateLine(dto.Quantity, dto.UnitPrice, dto.TaxRate);
        return new InvoiceLine
        {
            Id = id,
            InvoiceId = dto.InvoiceId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            TaxRate = dto.TaxRate,
            LineTotal = lineTotal,
            LineTax = lineTax
        };
    }
}
