using System.Globalization;

namespace OAS.UiLib.Core.Models.Purchasing;

/// <summary>
/// Presentation-only data for the purchase invoice list. Payment and receipt matching
/// are not inferred from invoice status; only persisted invoice states are displayed.
/// </summary>
public sealed record UiPurchaseInvoiceCardItem(
    Guid Id,
    string Code,
    string SupplierName,
    string? SupplierInvoiceCode,
    byte Status,
    string StatusText,
    decimal TotalAmount,
    string? CurrencyNameAr,
    string? CurrencyCode,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    int LineCount,
    int? MatchingAllocationCount = null,
    int? UnresolvedMatchingAllocationCount = null)
{
    public string CurrencyDisplayName => !string.IsNullOrWhiteSpace(CurrencyNameAr)
        ? CurrencyNameAr.Trim()
        : (string.IsNullOrWhiteSpace(CurrencyCode) ? "العملة غير محددة" : CurrencyCode.Trim());

    public string TotalAmountDisplay => TotalAmount.ToString("#,##0.############################", CultureInfo.InvariantCulture);
    public string InvoiceDateDisplay => FormatDate(InvoiceDate);
    public string? DueDateDisplay => DueDate is { } due ? FormatDate(due) : null;

    public string StatusClass => Status switch
    {
        2 => "is-confirmed", 3 => "is-matching", 4 => "is-posted", 5 => "is-cancelled", _ => "is-draft"
    };

    public string MatchingText => MatchingAllocationCount switch
    {
        null => "حالة المطابقة غير متاحة",
        0 => "لم تبدأ المطابقة",
        _ when UnresolvedMatchingAllocationCount is > 0 => "فروقات قيد المراجعة",
        _ => "مطابقة مكتملة"
    };

    public string MatchingClass => MatchingAllocationCount switch
    {
        null => "is-unknown",
        0 => "is-unmatched",
        _ when UnresolvedMatchingAllocationCount is > 0 => "is-pending",
        _ => "is-matched"
    };

    public string NextStepText => Status switch
    {
        1 => "بانتظار استكمال الفاتورة",
        2 when MatchingAllocationCount == 0 => "شغّل المطابقة مع الاستلام",
        2 when UnresolvedMatchingAllocationCount is > 0 => "راجع فروقات المطابقة",
        2 when MatchingAllocationCount is > 0 => "المطابقة مكتملة، راجع الترحيل",
        2 => "راجع المطابقة قبل الترحيل",
        3 => "تحتاج مراجعة واعتماد الفروقات",
        4 => "تم ترحيل الفاتورة",
        5 => "فاتورة ملغاة",
        _ => "راجع تفاصيل الفاتورة"
    };

    private static string FormatDate(DateOnly date)
        => FormattableString.Invariant($"{date.Year:0000} / {date.Month:00} / {date.Day:00}");
}
