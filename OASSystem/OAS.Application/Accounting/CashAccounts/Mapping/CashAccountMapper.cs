using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Mapping;

public sealed class CashAccountMapper
    : ICrudMapper<
        CashAccount,
        Guid,
        CashAccountDto,
        CreateCashAccountRequest,
        UpdateCashAccountRequest>
{
    public CashAccount Create(CreateCashAccountRequest source)
    {
        return CashAccount.Create(
            Guid.NewGuid(),
            source.Code,
            source.Name,
            source.AccountId,
            source.IsDefault,
            source.IsActive);
    }

    public void Update(UpdateCashAccountRequest source, CashAccount destination)
    {
        destination.UpdateDetails(
            source.Code,
            source.Name,
            source.AccountId,
            source.IsDefault);

        destination.SetActive(source.IsActive);
    }

    public CashAccountDto ToRead(CashAccount source)
    {
        return new CashAccountDto(
            source.Id,
            source.Code,
            source.Name,
            source.AccountId,
            source.IsDefault,
            source.IsActive,
            source.RowVersion is not null ? Convert.ToBase64String(source.RowVersion) : string.Empty);
    }
}
