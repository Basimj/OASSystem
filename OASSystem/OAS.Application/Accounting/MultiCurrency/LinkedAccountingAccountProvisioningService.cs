using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.MultiCurrency;

public sealed class LinkedAccountingAccountProvisioningService(
    IRepository<Account, Guid> accounts,
    IReadRepository<AccountingSettings, Guid> settingsRepository,
    ISequenceNumberGenerator sequences) : ILinkedAccountingAccountProvisioningService
{
    public async Task<Account> ProvisionCashAccountAsync(string nameAr, bool isActive, DateOnly? effectiveDate = null, CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        return await ProvisionAssetSubledgerAsync(settings.CashParentAccountId, "CashLinkedAccount", "cash_parent_account_required", nameAr, isActive, effectiveDate, cancellationToken);
    }

    public async Task<Account> ProvisionBankAccountAsync(string nameAr, bool isActive, DateOnly? effectiveDate = null, CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        return await ProvisionAssetSubledgerAsync(settings.BankParentAccountId, "BankLinkedAccount", "bank_parent_account_required", nameAr, isActive, effectiveDate, cancellationToken);
    }

    public async Task<Account> ProvisionEmployeeAccountAsync(string nameAr, bool isActive, DateOnly? effectiveDate = null, CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        return await ProvisionAssetSubledgerAsync(settings.EmployeeParentAccountId, "EmployeeLinkedAccount", "employee_parent_account_required", nameAr, isActive, effectiveDate, cancellationToken);
    }

    public async Task SynchronizeAsync(Guid accountId, string nameAr, bool isActive, CancellationToken cancellationToken = default)
    {
        var account = await accounts.GetForUpdateAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);
        account.UpdateDetails(
            account.Code,
            nameAr,
            account.NameEn,
            account.ParentAccountId,
            account.Level,
            account.AccountClass,
            account.AccountType,
            account.NormalBalance,
            account.IsPostingAccount,
            account.IsControlAccount,
            account.AllowManualPosting,
            account.EffectiveDate);
        account.SetActive(isActive);
        accounts.Update(account);
    }

    private async Task<Account> ProvisionAssetSubledgerAsync(
        Guid? parentId,
        string sequenceName,
        string missingCode,
        string nameAr,
        bool isActive,
        DateOnly? effectiveDate,
        CancellationToken ct)
    {
        if (!parentId.HasValue || parentId.Value == Guid.Empty)
            throw new ConflictException(missingCode, "يجب تحديد الحساب الرئيسي في إعدادات المحاسبة أولاً.");

        var parent = await accounts.GetForUpdateAsync(parentId.Value, ct)
            ?? throw new ConflictException(missingCode, "الحساب الرئيسي المحدد في إعدادات المحاسبة غير موجود.");

        var valid = parent.IsActive && parent.IsControlAccount && parent.AccountType == AccountType.Control &&
                    parent.AccountClass == AccountClass.Asset && parent.NormalBalance == NormalBalance.Debit;
        if (!valid)
            throw new ConflictException("linked_account_parent_invalid", "الحساب الرئيسي يجب أن يكون حساب تحكم نشطًا من الأصول بطبيعة مدينة.");
        if (parent.Level == byte.MaxValue)
            throw new ConflictException("account_level_exhausted", "لا يمكن إنشاء مستوى فرعي إضافي تحت الحساب المحدد.");
        if (string.IsNullOrWhiteSpace(parent.Code) || !parent.Code.All(char.IsDigit) || parent.Code.Length > 26)
            throw new ConflictException("linked_account_parent_code_invalid", "كود الحساب الرئيسي يجب أن يكون رقميًا وألا يتجاوز 26 خانة.");

        string code;
        do
        {
            var next = await sequences.NextAsync(sequenceName, ct);
            code = $"{parent.Code}{next:0000}";
        }
        while (await accounts.CountAsync(new Specification<Account>().Where(x => x.Code == code), ct) > 0);

        var account = Account.Create(
            Guid.NewGuid(), code, nameAr, null, parent.Id, (byte)(parent.Level + 1),
            AccountClass.Asset, AccountType.Subledger, NormalBalance.Debit,
            isPostingAccount: true, isControlAccount: false, allowManualPosting: false,
            isSystemAccount: false, isActive: isActive, effectiveDate: effectiveDate);
        await accounts.AddAsync(account, ct);
        return account;
    }

    private async Task<AccountingSettings> GetSettingsAsync(CancellationToken ct) =>
        await settingsRepository.GetByIdAsync(AccountingSettings.SingletonId, ct)
        ?? throw new ConflictException("accounting_settings_required", "يجب إعداد المحاسبة والعملة الأساسية أولاً.");
}
