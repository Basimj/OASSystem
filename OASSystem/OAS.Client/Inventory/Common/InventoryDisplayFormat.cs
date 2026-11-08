using System;
using System.Globalization;

namespace OAS.Client.Inventory.Common;

/// <summary>
/// Display-only formatting for RTL inventory screens. Persisted numbers, API
/// parameters and native date-input values must continue using their original types.
/// </summary>
public static class InventoryDisplayFormat
{
    private const string LeftToRightIsolate = "\u2066";
    private const string PopDirectionalIsolate = "\u2069";
    private static string Isolate(string value) => LeftToRightIsolate + value + PopDirectionalIsolate;

    // Show every meaningful decimal digit (decimal supports up to 28 digits),
    // but no unnecessary zero padding, even on weighted-average unit costs.
    public static string Number(decimal value) =>
        value.ToString("#,##0.############################", CultureInfo.InvariantCulture);

    // Use ungrouped, culture-independent values only in HTML number inputs.
    public static string InputNumber(decimal value) =>
        value.ToString("0.############################", CultureInfo.InvariantCulture);

    public static string Date(DateOnly value) =>
        Isolate(value.ToString("yyyy / MM / dd", CultureInfo.InvariantCulture));

    public static string Date(DateTimeOffset value) =>
        Isolate(value.ToString("yyyy / MM / dd", CultureInfo.InvariantCulture));

    public static string DateTime(DateTimeOffset value) =>
        Isolate(value.ToString("yyyy / MM / dd  HH:mm", CultureInfo.InvariantCulture));

    public static string DateTime(DateTimeOffset? value, string empty = "—") =>
        value.HasValue ? DateTime(value.Value) : empty;
}
