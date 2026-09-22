using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Accounting.CustomerAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CustomerAccounts.Mapping;

public sealed class CustomerAccountMapper
    : ICrudMapper<
        CustomerAccount,
        Guid,
        CustomerAccountDto,
        CreateCustomerAccountRequest,
        UpdateCustomerAccountRequest>
{
    public CustomerAccount Create(CreateCustomerAccountRequest source)
    {
        return CustomerAccount.Create(
            Guid.NewGuid(),
            source.CustomerId,
            source.AccountId,
            source.ControlAccountId,
            source.IsActive);
    }

    public void Update(UpdateCustomerAccountRequest source, CustomerAccount destination)
    {
        destination.UpdateAccounts(
            source.AccountId,
            source.ControlAccountId);
    }

    public CustomerAccountDto ToRead(CustomerAccount source)
    {
        return new CustomerAccountDto(
            source.Id,
            source.CustomerId,
            source.AccountId,
            source.ControlAccountId,
            source.IsActive,
            source.CreatedAtUtc,
            source.RowVersion is not null ? Convert.ToBase64String(source.RowVersion) : string.Empty);
    }
}
