using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.UiLib.Core.Models;

namespace OAS.Client.Accounting.Common;

public static class AccountingLookupEligibility
{
    public static UiLookupItem ToAccountLookup(
        AccountDto account,
        AccountingAccountEligibilityContext context,
        string? eligibleText = null)
    {
        var eligibility = AccountingAccountEligibility.Evaluate(account, context);
        var secondary = eligibility.IsEligible
            ? eligibleText ?? BuildEligibleAccountText(account)
            : eligibility.Reason;

        if (!string.IsNullOrWhiteSpace(account.NameEn))
            secondary = string.IsNullOrWhiteSpace(secondary)
                ? account.NameEn
                : $"{account.NameEn} · {secondary}";

        return new UiLookupItem(
            account.Id.ToString("D"),
            $"{account.Code} - {account.NameAr}",
            secondary,
            "fa-solid fa-folder-tree",
            Disabled: !eligibility.IsEligible);
    }

    public static UiLookupItem ToCashAccountLookup(
        CashAccountDto cash,
        AccountDto? glAccount,
        Guid? requiredCurrencyId = null)
    {
        var reason = GetMoneyAccountInvalidReason(
            cash.IsActive,
            cash.CurrencyId,
            requiredCurrencyId,
            glAccount,
            "الصندوق");

        var secondary = reason ?? (cash.IsDefault ? "الصندوق الافتراضي" : "صندوق صالح للتسوية");
        return new UiLookupItem(
            cash.Id.ToString("D"),
            $"{cash.Code} - {cash.Name}",
            secondary,
            "fa-solid fa-vault",
            Disabled: reason is not null);
    }

    public static UiLookupItem ToBankAccountLookup(
        BankAccountDto bank,
        AccountDto? glAccount,
        Guid? requiredCurrencyId = null)
    {
        var reason = GetMoneyAccountInvalidReason(
            bank.IsActive,
            bank.CurrencyId,
            requiredCurrencyId,
            glAccount,
            "الحساب البنكي");

        var secondary = reason ?? bank.AccountNumber;
        return new UiLookupItem(
            bank.Id.ToString("D"),
            $"{bank.BankName} - {bank.AccountName}",
            secondary,
            "fa-solid fa-building-columns",
            Disabled: reason is not null);
    }

    private static string BuildEligibleAccountText(AccountDto account) =>
        account.AllowManualPosting
            ? "صالح للترحيل والقيد اليدوي"
            : "صالح للترحيل الآلي";

    private static string? GetMoneyAccountInvalidReason(
        bool isActive,
        Guid? currencyId,
        Guid? requiredCurrencyId,
        AccountDto? glAccount,
        string label)
    {
        if (!isActive)
            return $"{label} غير نشط.";

        if (requiredCurrencyId.HasValue && currencyId != requiredCurrencyId)
            return $"عملة {label} لا تطابق عملة السطر.";

        if (glAccount is null)
            return $"تعذر التحقق من حساب GL المرتبط بـ{label}.";

        var eligibility = AccountingAccountEligibility.Evaluate(
            glAccount,
            AccountingAccountEligibilityContext.AutomaticPosting);

        return eligibility.IsEligible
            ? null
            : $"الحساب المحاسبي المرتبط بـ{label} غير صالح للترحيل: {eligibility.Reason}";
    }
}
