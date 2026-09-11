using Invoyz.Api.Domain.Common;

namespace Invoyz.Api.Domain.Invoices;

public sealed class InvoiceValidator : IValidator<Invoice>
{
    private const int InvoiceNumberMaxLength = 50;

    public ValidationResult Validate(Invoice entity)
    {
        var errors = new List<string>();

        if (entity.Id == Guid.Empty)
            errors.Add("Id is required.");

        if (string.IsNullOrWhiteSpace(entity.InvoiceNumber))
            errors.Add("InvoiceNumber is required.");
        else if (entity.InvoiceNumber.Length > InvoiceNumberMaxLength)
            errors.Add($"InvoiceNumber must not exceed {InvoiceNumberMaxLength} characters.");

        if (entity.CustomerId == Guid.Empty)
            errors.Add("CustomerId is required.");

        if (entity.IssueDate is null)
            errors.Add("IssueDate is required and must be a valid date.");

        if (entity.DueDate is null)
            errors.Add("DueDate is required and must be a valid date.");
        else if (entity.IssueDate is not null && entity.DueDate < entity.IssueDate)
            errors.Add("DueDate must be greater than or equal to IssueDate.");

        if (!Enum.IsDefined(entity.Status))
            errors.Add("Status must be a valid value (Draft, Sent, Paid, Overdue).");

        if (entity.SubTotal < 0)
            errors.Add("SubTotal must be greater than or equal to 0.");

        if (entity.TotalTax < 0)
            errors.Add("TotalTax must be greater than or equal to 0.");

        if (entity.GrandTotal < 0)
            errors.Add("GrandTotal must be greater than or equal to 0.");

        return errors.Count == 0 ? ValidationResult.Success() : new ValidationResult(errors);
    }
}
