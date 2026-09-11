using Invoyz.Api.Application.DTOs.Customers;
using Invoyz.Api.Domain.Common;
using Invoyz.Api.Domain.Customers;

namespace Invoyz.Api.Application.Customers;

public sealed class CustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly IValidator<Customer> _validator;

    public CustomerService(ICustomerRepository repository, IValidator<Customer> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _repository.GetAllAsync(cancellationToken);
        return customers.Select(ToDto).ToList();
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken);
        return customer is null ? null : ToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CustomerDto dto, CancellationToken cancellationToken = default)
    {
        var customer = ToEntity(dto, Guid.NewGuid());
        Validate(customer);
        await _repository.AddAsync(customer, cancellationToken);
        return ToDto(customer);
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, CustomerDto dto, CancellationToken cancellationToken = default)
    {
        var customer = ToEntity(dto, id);
        Validate(customer);
        await _repository.UpdateAsync(customer, cancellationToken);
        return ToDto(customer);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    private void Validate(Customer customer)
    {
        var result = _validator.Validate(customer);
        if (!result.IsValid)
            throw new DomainValidationException(result.Errors);
    }

    private static CustomerDto ToDto(Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        Email = customer.Email,
        Address = customer.Address,
        VatNumber = customer.VatNumber
    };

    private static Customer ToEntity(CustomerDto dto, Guid id) => new()
    {
        Id = id,
        Name = dto.Name,
        Email = dto.Email,
        Address = dto.Address,
        VatNumber = dto.VatNumber
    };
}
