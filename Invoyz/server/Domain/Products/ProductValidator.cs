using Invoyz.Api.Domain.Common;

namespace Invoyz.Api.Domain.Products;

public sealed class ProductValidator : IValidator<Product>
{
    private const int NameMaxLength = 200;
    private const int DescriptionMaxLength = 1000;

    public ValidationResult Validate(Product entity)
    {
        var errors = new List<string>();

        if (entity.Id == Guid.Empty)
            errors.Add("Id is required.");

        var name = entity.Name.Trim();
        if (string.IsNullOrEmpty(name))
            errors.Add("Name is required.");
        else if (name.Length > NameMaxLength)
            errors.Add($"Name must not exceed {NameMaxLength} characters.");

        if (entity.Description is { Length: > DescriptionMaxLength })
            errors.Add($"Description must not exceed {DescriptionMaxLength} characters.");

        if (entity.UnitPrice < 0)
            errors.Add("UnitPrice must be greater than or equal to 0.");

        if (entity.TaxRate < 0 || entity.TaxRate > 100)
            errors.Add("TaxRate must be between 0 and 100.");

        return errors.Count == 0 ? ValidationResult.Success() : new ValidationResult(errors);
    }
}
