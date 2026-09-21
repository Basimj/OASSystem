using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Mapping;

public sealed class BankAccountMapper
    : ICrudMapper<
        BankAccount,
        Guid,
        BankAccountDto,
        CreateBankAccountRequest,
        UpdateBankAccountRequest>
{
    public BankAccount Create(CreateBankAccountRequest source)
    {
        return BankAccount.Create(
            Guid.NewGuid(),
            source.Code,
            source.BankName,
            source.AccountName,
            source.AccountNumber,
            source.IBAN,
            source.AccountId,
            source.IsActive);
    }

    public void Update(UpdateBankAccountRequest source, BankAccount destination)
    {
        destination.UpdateDetails(
            source.Code,
            source.BankName,
            source.AccountName,
            source.AccountNumber,
            source.IBAN,
            source.AccountId);

        destination.SetActive(source.IsActive);
    }

    public BankAccountDto ToRead(BankAccount source)
    {
        return new BankAccountDto(
            source.Id,
            source.Code,
            source.BankName,
            source.AccountName,
            source.AccountNumber,
            source.IBAN,
            source.AccountId,
            source.IsActive,
            source.RowVersion is not null ? Convert.ToBase64String(source.RowVersion) : string.Empty);
    }
}
