using System.Globalization;
using System.Text;

namespace OAS.UiLib.Core.Models.Sales;

/// <summary>
/// Converts manual lens measurements from a text input to invariant numeric values.
/// Handles western/Arabic digits and signs without depending on browser or UI locale.
/// </summary>
public static class OpticalInputNumberParser
{
    public static bool TryParseDecimal(string? input, out decimal? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(input)) return true;
        var normalized = Normalize(input);
        if (!decimal.TryParse(normalized,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var parsed))
            return false;
        value = parsed;
        return true;
    }

    public static bool TryParseAxis(string? input, out short? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(input)) return true;
        var normalized = Normalize(input);
        if (!short.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            || parsed is < 1 or > 180)
            return false;
        value = parsed;
        return true;
    }

    private static string Normalize(string input)
    {
        var trimmed = input.Trim();
        var builder = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            builder.Append(ch switch
            {
                >= '\u0660' and <= '\u0669' => (char)('0' + ch - '\u0660'),
                >= '\u06F0' and <= '\u06F9' => (char)('0' + ch - '\u06F0'),
                '\u2212' or '\uFE63' or '\uFF0D' => '-',
                '\u066B' or ',' => '.',
                _ => ch
            });
        }
        return builder.ToString();
    }
}
