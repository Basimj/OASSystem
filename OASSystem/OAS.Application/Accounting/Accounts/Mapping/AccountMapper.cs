using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Accounting.Accounts;
using OAS.Domain.Accounting.Entities;
using DomainAccountClass = OAS.Domain.Accounting.Enums.AccountClass;
using DomainAccountType = OAS.Domain.Accounting.Enums.AccountType;
using DomainNormalBalance = OAS.Domain.Accounting.Enums.NormalBalance;

namespace OAS.Application.Accounting.Accounts.Mapping;

public sealed class AccountMapper
    : ICrudMapper<
        Account,
        Guid,
        AccountDto,
        CreateAccountRequest,
        UpdateAccountRequest>
{
    public Account Create(CreateAccountRequest source)
    {
        return Account.Create(
            Guid.NewGuid(),
            source.Code,
            source.NameAr,
            source.NameEn,
            source.ParentAccountId,
            source.Level,
            (DomainAccountClass)source.AccountClass,
            (DomainAccountType)source.AccountType,
            (DomainNormalBalance)source.NormalBalance,
            source.IsPostingAccount,
            source.IsControlAccount,
            source.AllowManualPosting,
            source.IsSystemAccount,
            source.IsActive,
            source.EffectiveDate);
    }

    public void Update(
        UpdateAccountRequest source,
        Account destination)
    {
        destination.UpdateDetails(
            destination.Code,
            source.NameAr,
            source.NameEn,
            source.ParentAccountId,
            source.Level,
            (DomainAccountClass)source.AccountClass,
            (DomainAccountType)source.AccountType,
            (DomainNormalBalance)source.NormalBalance,
            source.IsPostingAccount,
            source.IsControlAccount,
            source.AllowManualPosting,
            source.EffectiveDate);

        destination.SetActive(source.IsActive);
    }

    public AccountDto ToRead(Account source)
    {
        return new AccountDto(
            source.Id,
            source.Code,
            source.NameAr,
            source.NameEn,
            source.ParentAccountId,
            source.Level,
            (OAS.Contracts.Accounting.Enums.AccountClass)(byte)source.AccountClass,
            (OAS.Contracts.Accounting.Enums.AccountType)(byte)source.AccountType,
            (OAS.Contracts.Accounting.Enums.NormalBalance)(byte)source.NormalBalance,
            source.IsPostingAccount,
            source.IsControlAccount,
            source.AllowManualPosting,
            source.IsSystemAccount,
            source.IsActive,
            source.EffectiveDate,
            Convert.ToBase64String(source.RowVersion));
    }
}