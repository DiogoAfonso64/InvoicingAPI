namespace Invoyz.Api.Domain.Common;

public interface IValidator<in T>
{
    ValidationResult Validate(T entity);
}
