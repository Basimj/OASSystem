using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Parties;

public sealed class PartyAccountProvisioningService(
    IRepository<Account, Guid> accounts,
    ISequenceNumberGenerator sequences) : IPartyAccountProvisioningService
{
    public Task<Account> ProvisionCustomerAccountAsync(
        Guid parentAccountId,
        string nameAr,
        string? nameEn,
        bool isActive,
        DateOnly? effectiveDate,
        CancellationToken ct = default) =>
        ProvisionAsync(parentAccountId, nameAr, nameEn, isActive, effectiveDate, true, ct);

    public Task<Account> ProvisionSupplierAccountAsync(
        Guid parentAccountId,
        string nameAr,
        string? nameEn,
        bool isActive,
        DateOnly? effectiveDate,
        CancellationToken ct = default) =>
        ProvisionAsync(parentAccountId, nameAr, nameEn, isActive, effectiveDate, false, ct);

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
                "الحساب الرئيسي المحدد غير موجود.");

        var valid =
            parent.IsActive &&
            parent.IsControlAccount &&
            parent.AccountType == AccountType.Control &&
            (customer
                ? parent.AccountClass == AccountClass.Asset &&
                  parent.NormalBalance == NormalBalance.Debit
                : parent.AccountClass == AccountClass.Liability &&
                  parent.NormalBalance == NormalBalance.Credit);

        if (!valid)
        {
            throw new ConflictException(
                customer
                    ? "accounting_customer_parent_invalid"
                    : "accounting_supplier_parent_invalid",
                customer
                    ? "الحساب الرئيسي المحدد غير صالح لحسابات العملاء."
                    : "الحساب الرئيسي المحدد غير صالح لحسابات الموردين.");
        }

        if (parent.Level == byte.MaxValue)
        {
            throw new ConflictException(
                "accounting_account_level_exhausted",
                "لا يمكن إنشاء مستوى فرعي إضافي تحت هذا الحساب.");
        }

        if (string.IsNullOrWhiteSpace(parent.Code) ||
            !parent.Code.All(char.IsDigit) ||
            parent.Code.Length > 26)
        {
            throw new ConflictException(
                "accounting_party_parent_code_invalid",
                "كود الحساب الرئيسي يجب أن يتكون من أرقام فقط وألا يتجاوز 26 خانة حتى يمكن إنشاء كود الحساب الفرعي.");
        }

        string code;

        do
        {
            var next = await sequences.NextAsync(
                customer
                    ? "CustomerAccountCodeSequence"
                    : "SupplierAccountCodeSequence",
                ct);

            // Party accounting accounts are numeric only. The parent account code
            // becomes the prefix, while the sequence supplies a unique child suffix.
            // Example: parent 110200 + sequence 1 => 1102000001.
            code = $"{parent.Code}{next:0000}";
        }
        while (await accounts.CountAsync(
            new Specification<Account>().Where(x => x.Code == code),
            ct) > 0);

        var account = Account.Create(
            Guid.NewGuid(),
            code,
            nameAr,
            nameEn,
            parent.Id,
            (byte)(parent.Level + 1),
            customer ? AccountClass.Asset : AccountClass.Liability,
            AccountType.Subledger,
            customer ? NormalBalance.Debit : NormalBalance.Credit,
            isPostingAccount: true,
            isControlAccount: false,
            allowManualPosting: false,
            isSystemAccount: false,
            isActive: isActive,
            effectiveDate: effectiveDate);

        await accounts.AddAsync(account, ct);
        return account;
    }

    public async Task SynchronizeAsync(
        Guid accountId,
        string nameAr,
        string? nameEn,
        bool? isActive,
        CancellationToken ct = default)
    {
        var account = await accounts.GetForUpdateAsync(accountId, ct)
            ?? throw new NotFoundException(nameof(Account), accountId);

        account.UpdateDetails(
            account.Code,
            nameAr,
            nameEn,
            account.ParentAccountId,
            account.Level,
            account.AccountClass,
            account.AccountType,
            account.NormalBalance,
            account.IsPostingAccount,
            account.IsControlAccount,
            account.AllowManualPosting,
            account.EffectiveDate);

        if (isActive.HasValue)
        {
            account.SetActive(isActive.Value);
        }

        accounts.Update(account);
    }
}
