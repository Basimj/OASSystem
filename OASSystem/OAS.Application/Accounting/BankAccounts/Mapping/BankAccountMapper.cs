using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Mapping;

public sealed class BankAccountMapper : ICrudMapper<BankAccount, Guid, BankAccountDto, CreateBankAccountRequest, UpdateBankAccountRequest>
{
    public BankAccount Create(CreateBankAccountRequest source) =>
        throw new NotSupportedException("Bank accounts require server-side linked-account provisioning. Use CreateBankAccountCommandHandler.");

    public void Update(UpdateBankAccountRequest source, BankAccount destination)
    {
        destination.UpdateDetails(destination.Code, source.BankName, source.AccountName, source.AccountNumber, source.IBAN, destination.AccountId, source.CurrencyId);
        destination.SetActive(source.IsActive);
    }

    public BankAccountDto ToRead(BankAccount source) => new(
        source.Id, source.Code, source.BankName, source.AccountName, source.AccountNumber,
        source.IBAN, source.AccountId, source.CurrencyId, source.IsActive,
        source.RowVersion is not null ? Convert.ToBase64String(source.RowVersion) : string.Empty);
}
