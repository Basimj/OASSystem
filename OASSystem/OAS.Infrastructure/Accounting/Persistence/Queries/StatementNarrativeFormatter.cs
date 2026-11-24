using System.Text.RegularExpressions;

namespace OAS.Infrastructure.Accounting.Persistence.Queries;

/// <summary>Customer-facing narrative, separate from the posted ledger line description.</summary>
public static class StatementNarrativeFormatter
{
    private static readonly (string Prefix, string Label)[] PostingLabels =
    [
        ("Customer AR realized FX adjustment", "تسوية فرق الصرف المحقق على ذمم العملاء"),
        ("Supplier AP realized FX adjustment", "تسوية فرق الصرف المحقق على ذمم الموردين"),
        ("Realized FX gain on customer settlement", "أرباح فروق الصرف المحققة عند تحصيل العميل"),
        ("Realized FX loss on customer settlement", "خسائر فروق الصرف المحققة عند تحصيل العميل"),
        ("Realized FX gain on supplier settlement", "أرباح فروق الصرف المحققة عند سداد المورد"),
        ("Realized FX loss on supplier settlement", "خسائر فروق الصرف المحققة عند سداد المورد"),
        ("Reverse customer realized FX settlement", "عكس تسوية فرق الصرف المحقق للعميل"),
        ("Reverse supplier realized FX settlement", "عكس تسوية فرق الصرف المحقق للمورد"),
        ("Customer realized FX settlement", "تسوية فرق الصرف المحقق للعميل"),
        ("Supplier realized FX settlement", "تسوية فرق الصرف المحقق للمورد"),
        ("COGS reversal /", "عكس تكلفة البضاعة المباعة"),
        ("COGS /", "تكلفة البضاعة المباعة"),
        ("OGS /", "تكلفة البضاعة المباعة"),
        ("Customer return /", "تخفيض ذمم العملاء نتيجة مرتجع مبيعات"),
        ("Sales tax reversal /", "عكس ضريبة المبيعات"),
        ("Sales tax /", "ضريبة المبيعات"),
        ("Sales revenue reversal /", "عكس إيرادات المبيعات"),
        ("Sales revenue /", "إيرادات المبيعات"),
        ("Customer /", "ذمم العملاء"),
        ("Inventory purchase return ", "إخراج بضاعة مرتجعة إلى المورد"),
        ("Inventory return", "إعادة البضاعة إلى المخزون"),
        ("Inventory receipt", "إثبات دخول المشتريات إلى المخزون"),
        ("Inventory /", "حركة مخزون مرتبطة بفاتورة مبيعات"),
        ("GRNI clearance ", "تسوية استلامات المشتريات غير المفوترة"),
        ("GRNI reversal ", "عكس استحقاق استلام مشتريات غير مفوتر"),
        ("GRNI ", "إثبات استحقاق مشتريات مستلمة وغير مفوترة"),
        ("Purchase tax reversal ", "عكس ضريبة المشتريات"),
        ("Purchase tax ", "ضريبة المشتريات"),
        ("Purchase price variance ", "فرق سعر المشتريات"),
        ("Purchase return variance ", "فرق قيمة مرتجع المشتريات"),
        ("Supplier payable ", "إثبات ذمم الموردين"),
        ("Supplier return ", "تخفيض ذمم الموردين نتيجة مرتجع مشتريات"),
        ("Sales invoice ", "فاتورة مبيعات"),
        ("Sales return ", "مرتجع مبيعات"),
        ("Purchase receipt ", "استلام مشتريات"),
        ("Purchase invoice ", "فاتورة مشتريات"),
        ("Purchase return ", "مرتجع مشتريات"),
        ("Receipt voucher ", "سند قبض"),
        ("Payment voucher ", "سند صرف")
    ];

    public static string Resolve(string? documentNarrative, string? reference, string? sourceType,
        string? lineDescription, string? journalDescription, bool manual = false)
    {
        // Never translate user-entered narratives, including English text.
        if (!string.IsNullOrWhiteSpace(documentNarrative)) return documentNarrative.Trim();
        if (manual) return FirstText(lineDescription, journalDescription) ?? "قيد يومية";

        var journal = journalDescription?.Trim();
        var line = lineDescription?.Trim();
        var journalLabel = Translate(journal);
        var lineLabel = Translate(line);
        var type = sourceType?.Trim().ToLowerInvariant();
        var typeLabel = type switch
        {
            "salesinvoice" => "فاتورة مبيعات",
            "salesreturn" => "مرتجع مبيعات",
            "purchasereceipt" => "استلام مشتريات",
            "purchaseinvoice" => "فاتورة مشتريات",
            "purchasereturn" => "مرتجع مشتريات",
            "receiptvoucher" => "سند قبض",
            "paymentvoucher" => "سند صرف",
            "inventorytransaction" => "حركة مخزون",
            "customersettlementfx" => "تسوية فرق الصرف المحقق للعميل",
            "customersettlementfxreversal" => "عكس تسوية فرق الصرف المحقق للعميل",
            "suppliersettlementfx" => "تسوية فرق الصرف المحقق للمورد",
            "suppliersettlementfxreversal" => "عكس تسوية فرق الصرف المحقق للمورد",
            _ => null
        };
        // Prefer a real journal narrative when no source note exists. Do not show
        // generated allocation IDs in its place. For generated line labels, preserve
        // the accounting role even when no source document number was resolved.
        if (!string.IsNullOrWhiteSpace(journal) && journalLabel is null && !ContainsGuid(journal))
            return journal;
        if (!string.IsNullOrWhiteSpace(line) && lineLabel is null && !ContainsGuid(line))
            return line;
        var label = lineLabel ?? journalLabel ?? typeLabel;
        if (label is not null)
            return string.IsNullOrWhiteSpace(reference) ? label : $"{label} — {reference}";
        // Unknown sources: preserve a genuine narrative, but not a technical ID.
        return "قيد محاسبي";
    }

    private static string? Translate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        const string reversal = "Reversal: ";
        if (text.StartsWith(reversal, StringComparison.OrdinalIgnoreCase))
        {
            var translated = Translate(text[reversal.Length..]);
            if (translated is not null) return $"عكس قيد — {translated}";
        }
        foreach (var (prefix, label) in PostingLabels)
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return label;
        return null;
    }

    private static bool ContainsGuid(string text) => Regex.IsMatch(text,
        @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b");

    private static string? FirstText(string? first, string? second) =>
        !string.IsNullOrWhiteSpace(first) ? first.Trim() :
        !string.IsNullOrWhiteSpace(second) ? second.Trim() : null;
}
