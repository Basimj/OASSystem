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
    ExchangeLoss = 10,
    TaxPayable = 11,
    SalesRevenue = 12,
    Inventory = 13,
    CostOfGoodsSold = 14,
    RetainedEarnings = 15,
    GoodsReceivedNotInvoiced = 16,
    PurchaseTax = 17,
    PurchasePriceVariance = 18,
    CustomerAdvances = 19
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
            AccountingAccountEligibilityContext.TaxPayable => EvaluateTaxPayable(account),
            AccountingAccountEligibilityContext.CustomerAdvances => EvaluateCustomerAdvances(account),
            AccountingAccountEligibilityContext.SalesRevenue => EvaluateSalesRevenue(account),
            AccountingAccountEligibilityContext.Inventory => EvaluateInventory(account),
            AccountingAccountEligibilityContext.CostOfGoodsSold => EvaluateCostOfGoodsSold(account),
            AccountingAccountEligibilityContext.RetainedEarnings => EvaluateRetainedEarnings(account),
            AccountingAccountEligibilityContext.GoodsReceivedNotInvoiced => EvaluateGrni(account),
            AccountingAccountEligibilityContext.PurchaseTax => EvaluatePurchaseTax(account),
            AccountingAccountEligibilityContext.PurchasePriceVariance => EvaluatePurchasePriceVariance(account),
            _ => AccountingAccountEligibilityResult.Eligible
        };
    }


    private static AccountingAccountEligibilityResult EvaluateSalesRevenue(AccountDto account)
    {
        var posting = EvaluateAutomaticPosting(account);
        if (!posting.IsEligible)
            return posting;

        if (account.AccountClass != AccountClass.Revenue)
            return Invalid("sales_revenue_wrong_class", "يجب أن يكون حساب إيرادات المبيعات من حسابات الإيرادات.");

        if (account.NormalBalance != NormalBalance.Credit)
            return Invalid("sales_revenue_wrong_balance", "يجب أن تكون طبيعة حساب إيرادات المبيعات دائنة.");

        return AccountingAccountEligibilityResult.Eligible;
    }

    private static AccountingAccountEligibilityResult EvaluateInventory(AccountDto account)
    {
        var posting = EvaluateAutomaticPosting(account);
        if (!posting.IsEligible)
            return posting;

        if (account.AccountClass != AccountClass.Asset)
            return Invalid("inventory_wrong_class", "يجب أن يكون حساب المخزون من حسابات الأصول.");

        if (account.NormalBalance != NormalBalance.Debit)
            return Invalid("inventory_wrong_balance", "يجب أن تكون طبيعة حساب المخزون مدينة.");

        return AccountingAccountEligibilityResult.Eligible;
    }

    private static AccountingAccountEligibilityResult EvaluateCostOfGoodsSold(AccountDto account)
    {
        var posting = EvaluateAutomaticPosting(account);
        if (!posting.IsEligible)
            return posting;

        if (account.AccountClass != AccountClass.Expense)
            return Invalid("cogs_wrong_class", "يجب أن يكون حساب تكلفة البضاعة المباعة من حسابات المصروفات.");

        if (account.NormalBalance != NormalBalance.Debit)
            return Invalid("cogs_wrong_balance", "يجب أن تكون طبيعة حساب تكلفة البضاعة المباعة مدينة.");

        return AccountingAccountEligibilityResult.Eligible;
    }

    private static AccountingAccountEligibilityResult EvaluateGrni(AccountDto account)
    {
        var posting = EvaluateAutomaticPosting(account);
        if (!posting.IsEligible) return posting;
        if (account.AccountClass != AccountClass.Liability)
            return Invalid("grni_wrong_class", "يجب أن يكون حساب بضاعة مستلمة غير مفوترة من حسابات الالتزامات.");
        if (account.NormalBalance != NormalBalance.Credit)
            return Invalid("grni_wrong_balance", "يجب أن تكون طبيعة حساب بضاعة مستلمة غير مفوترة دائنة.");
        return AccountingAccountEligibilityResult.Eligible;
    }

    private static AccountingAccountEligibilityResult EvaluatePurchaseTax(AccountDto account)
    {
        var posting = EvaluateAutomaticPosting(account);
        if (!posting.IsEligible) return posting;
        if (account.AccountClass != AccountClass.Asset)
            return Invalid("purchase_tax_wrong_class", "يجب أن يكون حساب ضريبة المشتريات من حسابات الأصول.");
        if (account.NormalBalance != NormalBalance.Debit)
            return Invalid("purchase_tax_wrong_balance", "يجب أن تكون طبيعة حساب ضريبة المشتريات مدينة.");
        return AccountingAccountEligibilityResult.Eligible;
    }

    private static AccountingAccountEligibilityResult EvaluatePurchasePriceVariance(AccountDto account)
    {
        var posting = EvaluateAutomaticPosting(account);
        if (!posting.IsEligible) return posting;
        if (account.AccountClass != AccountClass.Expense)
            return Invalid("purchase_price_variance_wrong_class", "يجب أن يكون حساب فرق سعر المشتريات من حسابات المصروفات.");
        if (account.NormalBalance != NormalBalance.Debit)
            return Invalid("purchase_price_variance_wrong_balance", "يجب أن تكون طبيعة حساب فرق سعر المشتريات مدينة.");
        return AccountingAccountEligibilityResult.Eligible;
    }

    private static AccountingAccountEligibilityResult EvaluateRetainedEarnings(AccountDto account)
    {
        var posting = EvaluateAutomaticPosting(account);
        if (!posting.IsEligible)
            return posting;

        if (account.AccountClass != AccountClass.Equity)
            return Invalid("retained_earnings_wrong_class", "يجب أن يكون حساب الأرباح المحتجزة من حسابات حقوق الملكية.");

        if (account.NormalBalance != NormalBalance.Credit)
            return Invalid("retained_earnings_wrong_balance", "يجب أن تكون طبيعة حساب الأرباح المحتجزة دائنة.");

        return AccountingAccountEligibilityResult.Eligible;
    }


    private static AccountingAccountEligibilityResult EvaluateCustomerAdvances(AccountDto account)
    {
        var posting = EvaluateAutomaticPosting(account);
        if (!posting.IsEligible)
            return posting;

        if (account.AccountClass != AccountClass.Liability)
            return Invalid("customer_advances_wrong_class", "يجب أن يكون حساب دفعات مقدمة من العملاء من حسابات الالتزامات.");

        if (account.NormalBalance != NormalBalance.Credit)
            return Invalid("customer_advances_wrong_balance", "يجب أن تكون طبيعة حساب دفعات مقدمة من العملاء دائنة.");

        return AccountingAccountEligibilityResult.Eligible;
    }

    private static AccountingAccountEligibilityResult EvaluateTaxPayable(AccountDto account)
    {
        var posting = EvaluateAutomaticPosting(account);
        if (!posting.IsEligible)
            return posting;

        if (account.AccountClass != AccountClass.Liability)
            return Invalid("tax_payable_wrong_class", "يجب أن يكون حساب الضرائب المستحقة من حسابات الالتزامات.");

        if (account.NormalBalance != NormalBalance.Credit)
            return Invalid("tax_payable_wrong_balance", "يجب أن تكون طبيعة حساب الضرائب المستحقة دائنة.");

        return AccountingAccountEligibilityResult.Eligible;
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
