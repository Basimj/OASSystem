using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Accounting.Enums;

namespace OAS.Client.Accounting.Common;

public enum AccountingAccountEligibilityContext
{
    DisplayOnly = 0,
    ManualJournal = 1,
    AutomaticPosting = 2,
    ExpenseAccount = 3,
    PostingProfile = 4,
    OtherCounterparty = 5,
    OtherSettlement = 6,
    AssetControlParent = 7,
    LiabilityControlParent = 8,
    ExchangeGain = 9,
    ExchangeLoss = 10
}

public sealed record AccountingAccountEligibilityResult(
    bool IsEligible,
    string? ReasonCode = null,
    string? Reason = null)
{
    public static readonly AccountingAccountEligibilityResult Eligible = new(true);
}

/// <summary>
/// Client-side mirror of accounting account eligibility rules used only to improve UX.
/// Domain/Application validation remains authoritative.
/// </summary>
public static class AccountingAccountEligibility
{
    public static AccountingAccountEligibilityResult Evaluate(
        AccountDto account,
        AccountingAccountEligibilityContext context)
    {
        ArgumentNullException.ThrowIfNull(account);

        return context switch
        {
            AccountingAccountEligibilityContext.DisplayOnly => AccountingAccountEligibilityResult.Eligible,
            AccountingAccountEligibilityContext.AssetControlParent => EvaluateControlParent(
                account, AccountClass.Asset, NormalBalance.Debit, "الأصول", "مدينة"),
            AccountingAccountEligibilityContext.LiabilityControlParent => EvaluateControlParent(
                account, AccountClass.Liability, NormalBalance.Credit, "الالتزامات", "دائنة"),
            AccountingAccountEligibilityContext.ManualJournal or
            AccountingAccountEligibilityContext.OtherCounterparty or
            AccountingAccountEligibilityContext.OtherSettlement => EvaluateManualPosting(account),
            AccountingAccountEligibilityContext.AutomaticPosting or
            AccountingAccountEligibilityContext.ExpenseAccount or
            AccountingAccountEligibilityContext.PostingProfile or
            AccountingAccountEligibilityContext.ExchangeGain or
            AccountingAccountEligibilityContext.ExchangeLoss => EvaluateAutomaticPosting(account),
            _ => AccountingAccountEligibilityResult.Eligible
        };
    }

    private static AccountingAccountEligibilityResult EvaluateManualPosting(AccountDto account)
    {
        var automatic = EvaluateAutomaticPosting(account);
        if (!automatic.IsEligible)
            return automatic;

        if (!account.AllowManualPosting)
            return Invalid("manual_posting_not_allowed", "الحساب لا يسمح بالقيد اليدوي.");

        return AccountingAccountEligibilityResult.Eligible;
    }

    private static AccountingAccountEligibilityResult EvaluateAutomaticPosting(AccountDto account)
    {
        if (!account.IsActive)
            return Invalid("account_inactive", "الحساب غير نشط.");

        if (account.AccountType == AccountType.Header)
            return Invalid("account_header", "حساب رئيسي ولا يقبل الترحيل المباشر.");

        if (!account.IsPostingAccount)
            return Invalid("account_not_posting", "الحساب غير مفعّل لاستقبال الترحيل.");

        return AccountingAccountEligibilityResult.Eligible;
    }

    private static AccountingAccountEligibilityResult EvaluateControlParent(
        AccountDto account,
        AccountClass requiredClass,
        NormalBalance requiredBalance,
        string classLabel,
        string balanceLabel)
    {
        if (!account.IsActive)
            return Invalid("account_inactive", "الحساب غير نشط.");

        if (account.AccountClass != requiredClass)
            return Invalid("parent_wrong_class", $"يجب أن يكون الحساب الرئيسي من حسابات {classLabel}.");

        if (account.NormalBalance != requiredBalance)
            return Invalid("parent_wrong_balance", $"يجب أن تكون طبيعة الحساب الرئيسي {balanceLabel}.");

        if (account.AccountType != AccountType.Control || !account.IsControlAccount)
            return Invalid("parent_not_control", "يجب أن يكون الحساب الرئيسي حساب تحكم.");

        return AccountingAccountEligibilityResult.Eligible;
    }

    private static AccountingAccountEligibilityResult Invalid(string code, string reason) =>
        new(false, code, reason);
}
