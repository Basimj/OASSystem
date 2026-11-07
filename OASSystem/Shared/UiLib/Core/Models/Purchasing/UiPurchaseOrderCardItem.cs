using System.Globalization;

namespace OAS.UiLib.Core.Models.Purchasing;

/// <summary>
/// List card data; quantities are summarized per order line, not across unlike units.
/// </summary>
public sealed record UiPurchaseOrderCardItem(
    Guid Id,
    string Code,
    string SupplierName,
    string? WarehouseName,
    string StatusText,
    byte Status,
    decimal TotalAmount,
    string? CurrencyCode,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    int CompletedLines,
    int StartedLines,
    int TotalLines,
    bool IsOverdue,
    string? CurrencyNameAr = null)
{
    public string CurrencyDisplayName => string.IsNullOrWhiteSpace(CurrencyNameAr) ? "العملة غير محددة" : CurrencyNameAr.Trim();
    // Keep meaningful fractional digits, without forcing trailing .00 or rounding away precision.
    public string TotalAmountDisplay => TotalAmount.ToString("#,##0.############################", CultureInfo.InvariantCulture);
    public string OrderDateDisplay => FormatCardDate(OrderDate);
    public string? ExpectedDeliveryDateDisplay => ExpectedDeliveryDate is { } due ? FormatCardDate(due) : null;

    private static string FormatCardDate(DateOnly value)
        => FormattableString.Invariant($"{value.Year:0000} / {value.Month:00} / {value.Day:00}");
    public int CompletedPercentage => TotalLines == 0 ? 0 : (int)Math.Round(100m * CompletedLines / TotalLines);
}
