using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Mapping;

public sealed class CashAccountMapper : ICrudMapper<CashAccount, Guid, CashAccountDto, CreateCashAccountRequest, UpdateCashAccountRequest>
{
    public CashAccount Create(CreateCashAccountRequest source) =>
        throw new NotSupportedException("Cash accounts require server-side linked-account provisioning. Use CreateCashAccountCommandHandler.");

    public void Update(UpdateCashAccountRequest source, CashAccount destination)
    {
        destination.UpdateDetails(destination.Code, source.Name, destination.AccountId, source.CurrencyId, source.IsDefault);
        destination.SetActive(source.IsActive);
    }

    public CashAccountDto ToRead(CashAccount source) => new(
        source.Id, source.Code, source.Name, source.AccountId, source.CurrencyId,
        source.IsDefault, source.IsActive,
        source.RowVersion is not null ? Convert.ToBase64String(source.RowVersion) : string.Empty);
}
