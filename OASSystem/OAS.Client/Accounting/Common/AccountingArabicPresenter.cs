using OAS.Contracts.Accounting.Enums;
using OAS.UiLib.Core.Models;

namespace OAS.Client.Accounting.Common;

public static class AccountingArabicPresenter
{
    public static string GetAccountClassText(AccountClass value) => value switch
    {
        AccountClass.Asset => "الأصول",
        AccountClass.Liability => "الخصوم",
        AccountClass.Equity => "حقوق الملكية",
        AccountClass.Revenue => "الإيرادات",
        AccountClass.Expense => "المصروفات",
        _ => value.ToString()
    };

    public static string GetAccountTypeText(AccountType value) => value switch
    {
        AccountType.Header => "حساب رئيسي",
        AccountType.Posting => "حساب ترحيل",
        AccountType.Control => "حساب تحكمي",
        AccountType.Subledger => "حساب فرعي",
        _ => value.ToString()
    };

    public static string GetNormalBalanceText(NormalBalance value) => value switch
    {
        NormalBalance.Debit => "مدين",
        NormalBalance.Credit => "دائن",
        _ => value.ToString()
    };

    public static string GetFiscalYearStatusText(FiscalYearStatus value) => value switch
    {
        FiscalYearStatus.Future => "مستقبلية",
        FiscalYearStatus.Open => "مفتوحة",
        FiscalYearStatus.Closing => "قيد الإقفال",
        FiscalYearStatus.Closed => "مغلقة",
        _ => value.ToString()
    };

    public static string GetFiscalPeriodStatusText(FiscalPeriodStatus value) => value switch
    {
        FiscalPeriodStatus.Open => "مفتوحة",
        FiscalPeriodStatus.SoftClosed => "إغلاق جزئي",
        FiscalPeriodStatus.Closed => "مغلقة",
        _ => value.ToString()
    };

    public static string GetJournalTypeText(JournalType value) => value switch
    {
        JournalType.Automatic => "تلقائي",
        JournalType.Manual => "يدوي",
        JournalType.Opening => "افتتاحي",
        JournalType.Closing => "إقفال",
        JournalType.Reversal => "عكسي",
        _ => value.ToString()
    };

    public static string GetJournalStatusText(JournalEntryStatus value) => value switch
    {
        JournalEntryStatus.Draft => "مسودة",
        JournalEntryStatus.PendingApproval => "بانتظار الاعتماد",
        JournalEntryStatus.Approved => "معتمد",
        JournalEntryStatus.Posted => "مرحّل",
        JournalEntryStatus.Reversed => "معكوس",
        _ => value.ToString()
    };

    public static string GetCashShiftStatusText(CashShiftStatus value) => value switch
    {
        CashShiftStatus.Open => "مفتوحة",
        CashShiftStatus.Closing => "قيد الإغلاق",
        CashShiftStatus.Closed => "مغلقة",
        CashShiftStatus.Approved => "معتمدة",
        _ => value.ToString()
    };

    public static string GetExpenseStatusText(ExpenseStatus value) => value switch
    {
        ExpenseStatus.Draft => "مسودة",
        ExpenseStatus.Approved => "معتمد",
        ExpenseStatus.Posted => "مرحّل",
        ExpenseStatus.Cancelled => "ملغى",
        _ => value.ToString()
    };

    public static string GetReceiptVoucherStatusText(ReceiptVoucherStatus value) => value switch
    {
        ReceiptVoucherStatus.Draft => "مسودة",
        ReceiptVoucherStatus.Approved => "معتمد",
        ReceiptVoucherStatus.Posted => "مرحّل",
        ReceiptVoucherStatus.Cancelled => "ملغى",
        _ => value.ToString()
    };

    public static string GetPaymentVoucherStatusText(PaymentVoucherStatus value) => value switch
    {
        PaymentVoucherStatus.Draft => "مسودة",
        PaymentVoucherStatus.Approved => "معتمد",
        PaymentVoucherStatus.Posted => "مرحّل",
        PaymentVoucherStatus.Cancelled => "ملغى",
        _ => value.ToString()
    };

    public static string GetReceiptPartyTypeText(ReceiptPartyType value) => value switch
    {
        ReceiptPartyType.Customer => "عميل",
        ReceiptPartyType.Other => "أخرى",
        _ => value.ToString()
    };

    public static string GetPaymentPartyTypeText(PaymentPartyType value) => value switch
    {
        PaymentPartyType.Supplier => "مورد",
        PaymentPartyType.Employee => "موظف",
        PaymentPartyType.Other => "أخرى",
        _ => value.ToString()
    };

    public static string GetPaymentMethodText(PaymentMethod value) => value switch
    {
        PaymentMethod.Cash => "نقداً",
        PaymentMethod.Card => "بطاقة",
        PaymentMethod.BankTransfer => "تحويل بنكي",
        PaymentMethod.Cheque => "شيك",
        PaymentMethod.Other => "أخرى",
        _ => value.ToString()
    };

    public static string GetSettlementPartyTypeText(SettlementPartyType value) => value switch
    {
        SettlementPartyType.Customer => "عميل",
        SettlementPartyType.Supplier => "مورد",
        SettlementPartyType.Employee => "موظف",
        SettlementPartyType.Other => "أخرى",
        _ => value.ToString()
    };

    public static string GetExchangeRateTypeText(ExchangeRateType value) => value switch
    {
        ExchangeRateType.Accounting => "محاسبي",
        _ => value.ToString()
    };

    public static string GetExchangeRateSourceText(ExchangeRateSource value) => value switch
    {
        ExchangeRateSource.System => "تلقائي",
        ExchangeRateSource.Manual => "يدوي",
        _ => value.ToString()
    };

    public static string GetPaymentSourceTypeText(PaymentSourceType value) => value switch
    {
        PaymentSourceType.ReceiptVoucher => "سند قبض",
        PaymentSourceType.PaymentVoucher => "سند صرف",
        PaymentSourceType.CustomerAdvance => "دفعة مقدمة من العميل",
        _ => value.ToString()
    };

    public static string GetAllocationTargetDocumentTypeText(AllocationTargetDocumentType value) => value switch
    {
        AllocationTargetDocumentType.SalesInvoice => "فاتورة مبيعات",
        AllocationTargetDocumentType.PurchaseInvoice => "فاتورة مشتريات",
        AllocationTargetDocumentType.DebitAdjustment => "إشعار مدين",
        AllocationTargetDocumentType.CreditAdjustment => "إشعار دائن",
        _ => value.ToString()
    };

    public static string GetPostingRoleText(string? role) => role switch
    {
        "Cash" => "النقدية",
        "Bank" => "البنك",
        "ARControl" => "حساب مراقبة العملاء",
        "APControl" => "حساب مراقبة الموردين",
        "SalesRevenue" => "إيرادات المبيعات",
        "SalesReturn" => "مردودات المبيعات",
        "SalesDiscount" => "خصم المبيعات",
        "Inventory" => "المخزون",
        "COGS" => "تكلفة المبيعات",
        "TaxPayable" => "الضرائب المستحقة",
        "InventoryGain" => "أرباح المخزون",
        "InventoryLoss" => "خسائر المخزون",
        "CustomerAdvances" => "دفعات مقدمة من العملاء",
        _ => role ?? "غير محدد"
    };

    // UI Select Options for forms
    public static readonly IReadOnlyList<UiSelectOption> AccountClassOptions =
    [
        new(nameof(AccountClass.Asset), "الأصول"),
        new(nameof(AccountClass.Liability), "الخصوم"),
        new(nameof(AccountClass.Equity), "حقوق الملكية"),
        new(nameof(AccountClass.Revenue), "الإيرادات"),
        new(nameof(AccountClass.Expense), "المصروفات")
    ];

    public static readonly IReadOnlyList<UiSelectOption> AccountTypeOptions =
    [
        new(nameof(AccountType.Header), "حساب رئيسي"),
        new(nameof(AccountType.Posting), "حساب ترحيل"),
        new(nameof(AccountType.Control), "حساب تحكمي"),
        new(nameof(AccountType.Subledger), "حساب فرعي")
    ];

    public static readonly IReadOnlyList<UiSelectOption> NormalBalanceOptions =
    [
        new(nameof(NormalBalance.Debit), "مدين"),
        new(nameof(NormalBalance.Credit), "دائن")
    ];

    public static readonly IReadOnlyList<UiSelectOption> FiscalYearStatusOptions =
    [
        new(nameof(FiscalYearStatus.Future), "مستقبلية"),
        new(nameof(FiscalYearStatus.Open), "مفتوحة"),
        new(nameof(FiscalYearStatus.Closing), "قيد الإقفال"),
        new(nameof(FiscalYearStatus.Closed), "مغلقة")
    ];

    public static readonly IReadOnlyList<UiSelectOption> FiscalPeriodStatusOptions =
    [
        new(nameof(FiscalPeriodStatus.Open), "مفتوحة"),
        new(nameof(FiscalPeriodStatus.SoftClosed), "إغلاق جزئي"),
        new(nameof(FiscalPeriodStatus.Closed), "مغلقة")
    ];

    public static readonly IReadOnlyList<UiSelectOption> JournalTypeOptions =
    [
        new(nameof(JournalType.Automatic), "تلقائي"),
        new(nameof(JournalType.Manual), "يدوي"),
        new(nameof(JournalType.Opening), "افتتاحي"),
        new(nameof(JournalType.Closing), "إقفال"),
        new(nameof(JournalType.Reversal), "عكسي")
    ];

    public static readonly IReadOnlyList<UiSelectOption> ReceiptPartyTypeOptions =
    [
        new(nameof(ReceiptPartyType.Customer), "عميل"),
        new(nameof(ReceiptPartyType.Other), "أخرى")
    ];

    public static readonly IReadOnlyList<UiSelectOption> PaymentPartyTypeOptions =
    [
        new(nameof(PaymentPartyType.Supplier), "مورد"),
        new(nameof(PaymentPartyType.Employee), "موظف"),
        new(nameof(PaymentPartyType.Other), "أخرى")
    ];

    public static readonly IReadOnlyList<UiSelectOption> PaymentMethodOptions =
    [
        new(nameof(PaymentMethod.Cash), "نقداً"),
        new(nameof(PaymentMethod.Card), "بطاقة"),
        new(nameof(PaymentMethod.BankTransfer), "تحويل بنكي"),
        new(nameof(PaymentMethod.Cheque), "شيك"),
        new(nameof(PaymentMethod.Other), "أخرى")
    ];


    public static readonly IReadOnlyList<UiSelectOption> SettlementPartyTypeOptions =
    [
        new(nameof(SettlementPartyType.Customer), "عميل"),
        new(nameof(SettlementPartyType.Supplier), "مورد"),
        new(nameof(SettlementPartyType.Employee), "موظف"),
        new(nameof(SettlementPartyType.Other), "أخرى")
    ];

    public static readonly IReadOnlyList<UiSelectOption> ExchangeRateTypeOptions =
    [
        new(nameof(ExchangeRateType.Accounting), "محاسبي")
    ];

    public static readonly IReadOnlyList<UiSelectOption> PaymentSourceTypeOptions =
    [
        new(nameof(PaymentSourceType.ReceiptVoucher), "سند قبض"),
        new(nameof(PaymentSourceType.PaymentVoucher), "سند صرف"),
        new(nameof(PaymentSourceType.CustomerAdvance), "دفعة مقدمة من العميل")
    ];

    public static readonly IReadOnlyList<UiSelectOption> AllocationTargetDocumentTypeOptions =
    [
        new(nameof(AllocationTargetDocumentType.SalesInvoice), "فاتورة مبيعات"),
        new(nameof(AllocationTargetDocumentType.PurchaseInvoice), "فاتورة مشتريات"),
        new(nameof(AllocationTargetDocumentType.DebitAdjustment), "إشعار مدين"),
        new(nameof(AllocationTargetDocumentType.CreditAdjustment), "إشعار دائن")
    ];

    public static readonly IReadOnlyList<UiSelectOption> PostingRoleOptions =
    [
        new("Cash", "النقدية"),
        new("Bank", "البنك"),
        new("ARControl", "حساب مراقبة العملاء"),
        new("APControl", "حساب مراقبة الموردين"),
        new("SalesRevenue", "إيرادات المبيعات"),
        new("SalesReturn", "مردودات المبيعات"),
        new("SalesDiscount", "خصم المبيعات"),
        new("Inventory", "المخزون"),
        new("COGS", "تكلفة المبيعات"),
        new("TaxPayable", "الضرائب المستحقة"),
        new("InventoryGain", "أرباح المخزون"),
        new("InventoryLoss", "خسائر المخزون"),
        new("CustomerAdvances", "دفعات مقدمة من العملاء")
    ];
}
