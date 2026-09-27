namespace OAS.API.Printing;

internal static class ArabicAmountTextFormatter
{
    private static readonly string[] Units =
    [
        "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة",
        "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر"
    ];

    private static readonly string[] Tens =
    ["", "", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون"];

    private static readonly string[] Hundreds =
    ["", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة"];

    public static string Format(decimal amount, string? currencyName)
    {
        amount = Math.Abs(amount);
        var integerPart = (long)decimal.Truncate(amount);
        var fraction = (int)Math.Round((amount - integerPart) * 100m, MidpointRounding.AwayFromZero);
        if (fraction == 100)
        {
            integerPart++;
            fraction = 0;
        }

        var words = NumberToWords(integerPart);
        var currency = string.IsNullOrWhiteSpace(currencyName) ? string.Empty : " " + currencyName.Trim();
        var fractionText = fraction > 0 ? $" و {fraction:00}/100" : string.Empty;
        return $"فقط {words}{currency}{fractionText} لا غير";
    }

    private static string NumberToWords(long value)
    {
        if (value == 0) return "صفر";
        if (value < 0) return "سالب " + NumberToWords(Math.Abs(value));

        var parts = new List<string>();
        AppendScale(parts, value / 1_000_000_000, "مليار", "ملياران", "مليارات");
        value %= 1_000_000_000;
        AppendScale(parts, value / 1_000_000, "مليون", "مليونان", "ملايين");
        value %= 1_000_000;
        AppendScale(parts, value / 1_000, "ألف", "ألفان", "آلاف");
        value %= 1_000;
        if (value > 0) parts.Add(UnderThousand((int)value));

        return string.Join(" و ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static void AppendScale(List<string> parts, long group, string singular, string dual, string plural)
    {
        if (group <= 0) return;
        if (group == 1)
        {
            parts.Add(singular);
            return;
        }
        if (group == 2)
        {
            parts.Add(dual);
            return;
        }

        var groupWords = UnderThousand((int)group);
        var scale = group is >= 3 and <= 10 ? plural : singular;
        parts.Add($"{groupWords} {scale}");
    }

    private static string UnderThousand(int value)
    {
        var parts = new List<string>();
        var hundreds = value / 100;
        var rest = value % 100;
        if (hundreds > 0) parts.Add(Hundreds[hundreds]);
        if (rest > 0) parts.Add(UnderHundred(rest));
        return string.Join(" و ", parts);
    }

    private static string UnderHundred(int value)
    {
        if (value < 20) return Units[value];
        var tens = value / 10;
        var units = value % 10;
        return units == 0 ? Tens[tens] : $"{Units[units]} و {Tens[tens]}";
    }
}
