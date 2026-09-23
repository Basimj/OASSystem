using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Parties;

public sealed class PartyAccountProvisioningService(
    IRepository<Account,Guid> accounts,
    ISequenceNumberGenerator sequences) : IPartyAccountProvisioningService
{
    public Task<Account> ProvisionCustomerAccountAsync(Guid parentAccountId,string nameAr,string? nameEn,bool isActive,DateOnly? effectiveDate,CancellationToken ct=default) =>
        ProvisionAsync(parentAccountId,nameAr,nameEn,isActive,effectiveDate,true,ct);

    public Task<Account> ProvisionSupplierAccountAsync(Guid parentAccountId,string nameAr,string? nameEn,bool isActive,DateOnly? effectiveDate,CancellationToken ct=default) =>
        ProvisionAsync(parentAccountId,nameAr,nameEn,isActive,effectiveDate,false,ct);

    private async Task<Account> ProvisionAsync(
      Guid parentId,
      string nameAr,
      string? nameEn,
      bool isActive,
      DateOnly? effectiveDate,
      bool customer,
      CancellationToken ct)
    {
        var parent = await accounts.GetForUpdateAsync(parentId, ct)
            ?? throw new ConflictException(
                "accounting_party_parent_not_found",
                "«·Õ”«» «·—∆Ì”Ì «·„Õœœ €Ì— „ÊÃÊœ.");

        var valid =
            parent.IsActive &&
            parent.IsControlAccount &&
            parent.AccountType == AccountType.Control &&
            (
                customer
                    ? parent.AccountClass == AccountClass.Asset &&
                      parent.NormalBalance == NormalBalance.Debit
                    : parent.AccountClass == AccountClass.Liability &&
                      parent.NormalBalance == NormalBalance.Credit
            );

        if (!valid)
        {
            throw new ConflictException(
                customer
                    ? "accounting_customer_parent_invalid"
                    : "accounting_supplier_parent_invalid",
                customer
                    ? "«·Õ”«» «·—∆Ì”Ì «·„Õœœ €Ì— ’«·Õ ·Õ”«»«  «·⁄„·«¡."
                    : "«·Õ”«» «·—∆Ì”Ì «·„Õœœ €Ì— ’«·Õ ·Õ”«»«  «·„Ê—œÌ‰.");
        }

        if (parent.Level == byte.MaxValue)
        {
            throw new ConflictException(
                "accounting_account_level_exhausted",
                "·« Ì„ﬂ‰ ≈‰‘«¡ „” ÊÏ ›—⁄Ì ≈÷«›Ì  Õ  Â–« «·Õ”«».");
        }

        // ﬂÊœ «·Õ”«» «·—∆Ì”Ì ÌÃ» √‰ ÌﬂÊ‰ —ﬁ„Ì«.
        if (string.IsNullOrWhiteSpace(parent.Code) ||
            !parent.Code.All(char.IsDigit))
        {
            throw new ConflictException(
                "accounting_party_parent_code_invalid",
                "ﬂÊœ «·Õ”«» «·—∆Ì”Ì ÌÃ» √‰ Ì ﬂÊ‰ „‰ √—ﬁ«„ ›ﬁÿ.");
        }

        string code;

        do
        {
            var next = await sequences.NextAsync(
                customer
                    ? "CustomerAccountCodeSequence"
                    : "SupplierAccountCodeSequence",
                ct);

            // „À«·:
            // Parent = 1102
            // next   = 1
            // Code   = 11020001
            code = $"{parent.Code}{next:0000}";
        }
        while (await accounts.CountAsync(
            new Specification<Account>()
                .Where(x => x.Code == code),
            ct) > 0);

        var account = Account.Create(
            Guid.NewGuid(),
            code,
            nameAr,
            nameEn,
            parent.Id,
            (byte)(parent.Level + 1),

            customer
                ? AccountClass.Asset
                : AccountClass.Liability,

            AccountType.Subledger,

            customer
                ? NormalBalance.Debit
                : NormalBalance.Credit,

            isPostingAccount: true,
            isControlAccount: false,
            allowManualPosting: false,
            isSystemAccount: false,
            isActive: isActive,
            effectiveDate: effectiveDate);

        await accounts.AddAsync(account, ct);

        return account;
    }
    public async Task SynchronizeAsync(Guid accountId,string nameAr,string? nameEn,bool? isActive,CancellationToken ct=default)
    {
        var account=await accounts.GetForUpdateAsync(accountId,ct) ?? throw new NotFoundException(nameof(Account),accountId);
        account.UpdateDetails(account.Code,nameAr,nameEn,account.ParentAccountId,account.Level,account.AccountClass,account.AccountType,
            account.NormalBalance,account.IsPostingAccount,account.IsControlAccount,account.AllowManualPosting,account.EffectiveDate);
        if(isActive.HasValue) account.SetActive(isActive.Value);
        accounts.Update(account);
    }
}
