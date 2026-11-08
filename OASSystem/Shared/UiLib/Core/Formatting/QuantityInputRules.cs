using System;
using System.Collections.Generic;

namespace OAS.UiLib.Core.Formatting;

/// <summary>
/// Quantity-entry hints. Only known discrete units are restricted to whole numbers.
/// Unknown units, measured units and pre-existing fractional values remain editable.
/// This does not change the decimal quantities or precision in the domain/database.
/// </summary>
public static class QuantityInputRules
{
    private static readonly HashSet<string> DiscreteUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "قطعة", "قطعه", "القطعة", "قطع", "حبة", "حبات", "عدد", "واحدة", "وحدة", "وحدات",
        "زوج", "أزواج", "علبة", "علب", "كرتون", "كراتين", "صندوق", "صناديق",
        "حزمة", "حزم", "طقم", "أطقم", "شريط", "أشرطة",
        "pc", "pcs", "piece", "pieces", "ea", "each", "unit", "units",
        "pair", "pairs", "pr", "box", "boxes", "bx", "ctn", "carton",
        "pack", "packs", "pkt", "set", "sets", "nos", "no"
    };

    public const string WholeNumberMessage = "هذه الوحدة تُحسب بأعداد صحيحة فقط؛ أدخل كمية دون كسور.";

    public static bool IsDiscreteUnit(string? codeOrName)
    {
        if (string.IsNullOrWhiteSpace(codeOrName)) return false;
        var normalized = codeOrName.Trim();
        if (DiscreteUnits.Contains(normalized)) return true;
        var pieces = normalized.Split(new[] { " - ", " – ", " — " }, StringSplitOptions.RemoveEmptyEntries);
        return pieces.Length == 2 && DiscreteUnits.Contains(pieces[0].Trim())
            && DiscreteUnits.Contains(pieces[1].Trim());
    }

    public static bool UsesWholeNumbers(string? code, string? name, decimal currentValue = 0m)
        => currentValue == decimal.Truncate(currentValue)
           && (IsDiscreteUnit(code) || IsDiscreteUnit(name));

    public static string StepForUnit(string? code, string? name, decimal currentValue = 0m)
        => UsesWholeNumbers(code, name, currentValue) ? "1" : "0.001";

    public static bool IsWhole(decimal value) => value == decimal.Truncate(value);
}
