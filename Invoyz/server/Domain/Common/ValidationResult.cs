namespace Invoyz.Api.Domain.Common;

public sealed class ValidationResult
{
    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public ValidationResult(IReadOnlyList<string> errors)
    {
        Errors = errors;
    }

    public static ValidationResult Success() => new(Array.Empty<string>());
}
