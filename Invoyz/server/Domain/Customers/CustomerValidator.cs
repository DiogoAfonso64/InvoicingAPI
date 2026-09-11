using System.Text.RegularExpressions;
using Invoyz.Api.Domain.Common;

namespace Invoyz.Api.Domain.Customers;

public sealed partial class CustomerValidator : IValidator<Customer>
{
    private const int NameMaxLength = 200;
    private const int EmailMaxLength = 254;
    private const int AddressMaxLength = 500;

    public ValidationResult Validate(Customer entity)
    {
        var errors = new List<string>();

        if (entity.Id == Guid.Empty)
            errors.Add("Id is required.");

        var name = entity.Name.Trim();
        if (string.IsNullOrEmpty(name))
            errors.Add("Name is required.");
        else if (name.Length > NameMaxLength)
            errors.Add($"Name must not exceed {NameMaxLength} characters.");

        if (string.IsNullOrWhiteSpace(entity.Email))
            errors.Add("Email is required.");
        else if (entity.Email.Length > EmailMaxLength)
            errors.Add($"Email must not exceed {EmailMaxLength} characters.");
        else if (!EmailRegex().IsMatch(entity.Email))
            errors.Add("Email must be a valid email address.");

        if (string.IsNullOrWhiteSpace(entity.Address))
            errors.Add("Address is required.");
        else if (entity.Address.Length > AddressMaxLength)
            errors.Add($"Address must not exceed {AddressMaxLength} characters.");

        if (string.IsNullOrWhiteSpace(entity.VatNumber))
            errors.Add("VatNumber is required.");
        else if (!VatNumberRegex().IsMatch(entity.VatNumber))
            errors.Add("VatNumber must match a valid VAT format (country prefix followed by digits).");

        return errors.Count == 0 ? ValidationResult.Success() : new ValidationResult(errors);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^[A-Z]{2}[0-9A-Z]{2,13}$")]
    private static partial Regex VatNumberRegex();
}
