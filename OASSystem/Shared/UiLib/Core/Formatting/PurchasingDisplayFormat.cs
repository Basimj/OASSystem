using System;
using System.Globalization;

namespace OAS.UiLib.Core.Formatting;

/// <summary>
/// Presentation-only date formatting for right-to-left purchasing screens.
/// Do not use these values for form inputs, API parameters, or persisted data.
/// LRI/PDI keep the year, month, day and time in order next to Arabic text.
/// </summary>
public static class PurchasingDisplayFormat
{
    private const string LeftToRightIsolate = "\u2066";
    private const string PopDirectionalIsolate = "\u2069";

    private static string Isolate(string value) => LeftToRightIsolate + value + PopDirectionalIsolate;

    public static string DisplayDateText(string? dateText, string empty = "—")
    {
        if (string.IsNullOrWhiteSpace(dateText)) return empty;
        // A date previously formatted for display already contains bidi isolation marks.
        if (dateText[0] == '\u2066') return dateText;
        if (DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date)) return DisplayDate(date);
        return dateText;
    }

    public static string DisplayShortDate(DateOnly value)
        => Isolate(value.ToString("dd/MM", CultureInfo.InvariantCulture));

    public static string DisplayDate(DateOnly value)
        => Isolate(value.ToString("yyyy / MM / dd", CultureInfo.InvariantCulture));

    public static string DisplayDate(DateOnly? value, string empty = "—")
        => value.HasValue ? DisplayDate(value.Value) : empty;

    public static string DisplayDate(DateTime value)
        => Isolate(value.ToString("yyyy / MM / dd", CultureInfo.InvariantCulture));

    public static string DisplayDate(DateTime? value, string empty = "—")
        => value.HasValue ? DisplayDate(value.Value) : empty;

    public static string DisplayDate(DateTimeOffset value)
        => Isolate(value.ToString("yyyy / MM / dd", CultureInfo.InvariantCulture));

    public static string DisplayDate(DateTimeOffset? value, string empty = "—")
        => value.HasValue ? DisplayDate(value.Value) : empty;

    public static string DisplayDateTime(DateTime value)
        => Isolate(value.ToString("yyyy / MM / dd  HH:mm", CultureInfo.InvariantCulture));

    public static string DisplayDateTime(DateTimeOffset value)
        => Isolate(value.ToString("yyyy / MM / dd  HH:mm", CultureInfo.InvariantCulture));
}
