using Invoyz.Api.Domain.Common;

namespace Invoyz.Api.Domain.InvoiceLines;

public sealed class InvoiceLineValidator : IValidator<InvoiceLine>
{
    public ValidationResult Validate(InvoiceLine entity)
    {
        var errors = new List<string>();

        if (entity.Id == Guid.Empty)
            errors.Add("Id is required.");

        if (entity.InvoiceId == Guid.Empty)
            errors.Add("InvoiceId is required and must reference an existing Invoice.");

        if (entity.ProductId == Guid.Empty)
            errors.Add("ProductId is required and must reference an existing Product.");

        if (entity.Quantity <= 0)
            errors.Add("Quantity must be greater than 0.");

        if (entity.UnitPrice < 0)
            errors.Add("UnitPrice must be greater than or equal to 0.");

        if (entity.TaxRate < 0 || entity.TaxRate > 100)
            errors.Add("TaxRate must be between 0 and 100.");

        if (entity.LineTotal < 0)
            errors.Add("LineTotal must be greater than or equal to 0.");

        if (entity.LineTax < 0)
            errors.Add("LineTax must be greater than or equal to 0.");

        return errors.Count == 0 ? ValidationResult.Success() : new ValidationResult(errors);
    }
}
