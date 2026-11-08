using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.FiscalPeriods;

namespace OAS.Client.Purchasing.Common;

/// <summary>
/// Gives receipt users a friendly explanation before posting. The server still
/// validates the period inside the posting transaction and remains authoritative.
/// </summary>
public static class PurchaseReceiptPostingPeriodValidation
{
    public static string? Validate(DateOnly postingDate, IReadOnlyCollection<FiscalPeriodDto> periods)
    {
        var covering = periods
            .Where(p => p.StartDate <= postingDate && p.EndDate >= postingDate)
            .Take(2)
            .ToArray();

        if (covering.Length == 0)
            return Explain("fiscal_period_not_found", postingDate);
        if (covering.Length > 1)
            return Explain("fiscal_period_overlap", postingDate);

        var period = covering[0];
        if (period.Status == FiscalPeriodStatus.Closed || period.InventoryLocked)
            return Explain("inventory_period_locked", postingDate);
        if (period.AccountingLocked)
            return Explain("fiscal_period_closed", postingDate);

        return null;
    }

    public static string? Explain(string? errorCode, DateOnly postingDate)
    {
        var date = "\u2066" + postingDate.ToString("yyyy/MM/dd") + "\u2069";
        return errorCode switch
        {
            "fiscal_period_not_found" =>
                $"لا توجد فترة مالية تغطي تاريخ الترحيل {date}. افتح المحاسبة ← السنوات المالية ← الفترات المالية، وأنشئ فترة تشمل هذا التاريخ، أو صحح تاريخ السند.",
            "fiscal_period_overlap" =>
                $"توجد أكثر من فترة مالية تغطي تاريخ الترحيل {date}. راجع تواريخ الفترات المالية وأزل التداخل قبل الترحيل.",
            "inventory_period_locked" =>
                $"تعذر ترحيل الاستلام بتاريخ {date} لأن الفترة المالية مغلقة أو قفل المخزون مفعّل. راجع حالة الفترة وقفل المخزون في المحاسبة.",
            "fiscal_period_closed" =>
                $"تعذر ترحيل الاستلام بتاريخ {date} لأن الفترة المالية مغلقة أو قفل المحاسبة مفعّل. راجع حالة الفترة وقفل المحاسبة في المحاسبة.",
            _ => null
        };
    }
}
