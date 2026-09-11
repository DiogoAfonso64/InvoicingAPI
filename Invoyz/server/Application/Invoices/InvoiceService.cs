using Invoyz.Api.Application.DTOs.Invoices;
using Invoyz.Api.Application.DTOs.InvoiceLines;
using Invoyz.Api.Domain.Common;
using Invoyz.Api.Domain.Invoices;
using Invoyz.Api.Domain.InvoiceLines;

namespace Invoyz.Api.Application.Invoices;

public sealed class InvoiceService
{
    private readonly IInvoiceRepository _repository;
    private readonly IValidator<Invoice> _validator;

    public InvoiceService(IInvoiceRepository repository, IValidator<Invoice> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<IReadOnlyList<InvoiceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await _repository.GetAllAsync(cancellationToken);
        return invoices.Select(ToDto).ToList();
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _repository.GetByIdAsync(id, cancellationToken);
        return invoice is null ? null : ToDto(invoice);
    }

    public async Task<InvoiceDto> CreateAsync(InvoiceDto dto, CancellationToken cancellationToken = default)
    {
        var invoice = ToEntity(dto, Guid.NewGuid());
        Validate(invoice);
        await _repository.AddAsync(invoice, cancellationToken);
        return ToDto(invoice);
    }

    public async Task<InvoiceDto> UpdateAsync(Guid id, InvoiceDto dto, CancellationToken cancellationToken = default)
    {
        var invoice = ToEntity(dto, id);
        Validate(invoice);
        await _repository.UpdateAsync(invoice, cancellationToken);
        return ToDto(invoice);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    private void Validate(Invoice invoice)
    {
        var result = _validator.Validate(invoice);
        if (!result.IsValid)
            throw new DomainValidationException(result.Errors);
    }

    private static InvoiceDto ToDto(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        CustomerId = invoice.CustomerId,
        IssueDate = invoice.IssueDate,
        DueDate = invoice.DueDate,
        Status = invoice.Status.ToString(),
        SubTotal = invoice.SubTotal,
        TotalTax = invoice.TotalTax,
        GrandTotal = invoice.GrandTotal,
        Lines = invoice.Lines.Select(ToDto).ToList()
    };

    private static Invoice ToEntity(InvoiceDto dto, Guid id)
    {
        var lines = dto.Lines.Select(ToEntity).ToList();
        var (subTotal, totalTax, grandTotal) = InvoiceCalculator.CalculateInvoice(lines);

        return new Invoice
        {
            Id = id,
            InvoiceNumber = dto.InvoiceNumber,
            CustomerId = dto.CustomerId,
            IssueDate = dto.IssueDate,
            DueDate = dto.DueDate,
            Status = ParseStatus(dto.Status),
            SubTotal = subTotal,
            TotalTax = totalTax,
            GrandTotal = grandTotal,
            Lines = lines
        };
    }

    private static InvoiceStatus ParseStatus(string status)
        => Enum.TryParse<InvoiceStatus>(status, ignoreCase: true, out var result) ? result : (InvoiceStatus)(-1);

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

    private static InvoiceLine ToEntity(InvoiceLineDto dto)
    {
        var (lineTotal, lineTax) = InvoiceCalculator.CalculateLine(dto.Quantity, dto.UnitPrice, dto.TaxRate);
        return new InvoiceLine
        {
            Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
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
