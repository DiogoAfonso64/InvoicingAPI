namespace Invoyz.Api.Application.DTOs.Customers;

public sealed record CustomerDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string VatNumber { get; init; } = string.Empty;
}
