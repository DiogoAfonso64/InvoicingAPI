namespace Invoyz.Api.Application.DTOs.Products;

public sealed record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TaxRate { get; init; }
}
