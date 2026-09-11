using Invoyz.Api.Domain.Invoices;

namespace Invoyz.Api.Domain.Customers;

public sealed class Customer
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public List<Invoice> Invoices { get; init; } = new();

    public string Diogo => "Diogo";
}
