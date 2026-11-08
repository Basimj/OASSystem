using System.Text.RegularExpressions;
using OAS.UiLib.Core.Formatting;

namespace OAS.UiLib.Core.Models.Purchasing;

/// <summary>
/// Explains receipt-entry errors in Arabic before the API request. The application
/// and domain validators still enforce the authoritative business rules.
/// </summary>
public static class UiPurchaseReceiptFormValidator
{
    public static IReadOnlyList<UiPurchaseReceiptLineModel> LinesToSubmit(UiPurchaseReceiptEditorModel receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        // Unreceived purchase-order lines are displayed for convenience, but must
        // not be sent: the API requires ReceivedQuantity > 0 for every submitted line.
        return receipt.Lines.Where(x => x.ReceivedQuantity != 0m || x.AcceptedQuantity != 0m || x.RejectedQuantity != 0m).ToArray();
    }

    public static IReadOnlyDictionary<string, string> Validate(UiPurchaseReceiptEditorModel receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!receipt.PurchaseOrderId.HasValue || receipt.PurchaseOrderId.Value == Guid.Empty)
            errors[nameof(receipt.PurchaseOrderId)] = "اختر أمر الشراء الذي تريد استلام البضاعة منه.";
        if (receipt.ReceiptDate == default)
            errors[nameof(receipt.ReceiptDate)] = "حدد تاريخ استلام البضاعة.";
        if (receipt.PostingDate == default)
            errors[nameof(receipt.PostingDate)] = "حدد تاريخ الترحيل الصحيح.";

        var selected = LinesToSubmit(receipt);
        if (selected.Count == 0)
            errors[nameof(receipt.Lines)] = "أدخل كمية مستلمة أكبر من صفر في صنف واحد على الأقل، ثم احفظ السند. يمكن ترك الأصناف غير المستلمة بصفر.";

        var seen = new HashSet<Guid>();
        foreach (var line in selected)
        {
            var index = receipt.Lines.IndexOf(line);
            var prefix = $"Lines[{index}].";
            var label = $"الصنف {index + 1}";
            if (line.PurchaseOrderLineId == Guid.Empty)
                errors[prefix + nameof(line.PurchaseOrderLineId)] = $"{label}: اختر أمر شراء صالحًا وأعد تحميل أصنافه.";
            else if (!seen.Add(line.PurchaseOrderLineId))
                errors[prefix + nameof(line.PurchaseOrderLineId)] = $"{label}: لا يمكن تكرار الصنف نفسه في سند الاستلام.";
            if (line.LineSequence <= 0)
                errors[prefix + nameof(line.LineSequence)] = $"{label}: ترتيب الصنف غير صحيح؛ أعد اختيار أمر الشراء.";
            if (line.ReceivedQuantity <= 0)
                errors[prefix + nameof(line.ReceivedQuantity)] = $"{label}: الكمية المستلمة يجب أن تكون أكبر من صفر؛ اترك الصنف صفرًا إذا لم تستلمه.";
            if (line.AcceptedQuantity < 0)
                errors[prefix + nameof(line.AcceptedQuantity)] = $"{label}: الكمية المقبولة لا يمكن أن تكون سالبة.";
            if (line.RejectedQuantity < 0)
                errors[prefix + nameof(line.RejectedQuantity)] = $"{label}: الكمية المرفوضة لا يمكن أن تكون سالبة.";
            if (QuantityInputRules.UsesWholeNumbers(null, line.PurchaseUnitName, line.OrderedQuantitySnapshot))
            {
                if (!QuantityInputRules.IsWhole(line.ReceivedQuantity))
                    errors[prefix + nameof(line.ReceivedQuantity)] = $"{label}: {QuantityInputRules.WholeNumberMessage}";
                if (!QuantityInputRules.IsWhole(line.AcceptedQuantity))
                    errors[prefix + nameof(line.AcceptedQuantity)] = $"{label}: {QuantityInputRules.WholeNumberMessage}";
                if (!QuantityInputRules.IsWhole(line.RejectedQuantity))
                    errors[prefix + nameof(line.RejectedQuantity)] = $"{label}: {QuantityInputRules.WholeNumberMessage}";
            }
            if (line.ActualUnitCost < 0)
                errors[prefix + nameof(line.ActualUnitCost)] = $"{label}: التكلفة الفعلية لا يمكن أن تكون سالبة.";
            if (line.AcceptedQuantity >= 0 && line.RejectedQuantity >= 0 &&
                line.AcceptedQuantity + line.RejectedQuantity != line.ReceivedQuantity)
                errors[prefix + nameof(line.AcceptedQuantity)] = $"{label}: مجموع المقبول والمرفوض يجب أن يساوي المستلم ({line.ReceivedQuantity:G29}).";
            if (line.AcceptedQuantity > line.RemainingReceivableQuantity)
                errors[prefix + nameof(line.AcceptedQuantity)] = $"{label}: الكمية المقبولة تتجاوز المتبقي في أمر الشراء ({line.RemainingReceivableQuantity:G29}).";
        }
        return errors;
    }

    /// <summary>When no rejected split was entered, treat the received amount as accepted by default.</summary>
    public static void SetReceivedQuantity(UiPurchaseReceiptLineModel line, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(line);
        var acceptAll = line.RejectedQuantity == 0m && line.AcceptedQuantity == line.ReceivedQuantity;
        line.ReceivedQuantity = amount;
        if (acceptAll) line.AcceptedQuantity = amount;
    }

    /// <summary>
    /// Convert FluentValidation property paths and codes into safe, field-specific
    /// Arabic errors. Indices refer to submitted (nonzero) lines, not every visible line.
    /// </summary>
    public static IReadOnlyDictionary<string, string> TranslateServerErrors(
        UiPurchaseReceiptEditorModel receipt, IReadOnlyDictionary<string, string[]> serverErrors)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        ArgumentNullException.ThrowIfNull(serverErrors);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var selected = LinesToSubmit(receipt);
        foreach (var (raw, codes) in serverErrors)
        {
            var field = raw.StartsWith("Request.", StringComparison.OrdinalIgnoreCase) ? raw[8..] : raw;
            var match = Regex.Match(field, @"^Lines\[(\d+)\](?:\.(\w+))?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (!int.TryParse(match.Groups[1].Value, out var index) || index < 0 || index >= selected.Count)
                {
                    result["Form"] = "رفض الخادم أحد بنود الاستلام. راجع الكميات وأعد المحاولة.";
                    continue;
                }
                var original = receipt.Lines.IndexOf(selected[index]);
                var name = match.Groups[2].Success ? match.Groups[2].Value :
                    (codes?.Contains("receipt_quantity_split_invalid", StringComparer.OrdinalIgnoreCase) == true ? "AcceptedQuantity" : "ReceivedQuantity");
                field = $"Lines[{original}].{name}";
            }
            else if (!field.Equals(nameof(receipt.PurchaseOrderId), StringComparison.OrdinalIgnoreCase) &&
                     !field.Equals(nameof(receipt.Lines), StringComparison.OrdinalIgnoreCase) &&
                     !field.Equals(nameof(receipt.ReceiptDate), StringComparison.OrdinalIgnoreCase) &&
                     !field.Equals(nameof(receipt.PostingDate), StringComparison.OrdinalIgnoreCase))
            {
                result["Form"] = "تعذر حفظ سند الاستلام بسبب بيانات غير صحيحة. راجع البنود وأعد المحاولة.";
                continue;
            }
            var code = codes?.FirstOrDefault() ?? string.Empty;
            result[field] = Explain(field, code);
        }
        return result;
    }

    private static string Explain(string field, string code)
    {
        if (field.Equals("PurchaseOrderId", StringComparison.OrdinalIgnoreCase))
            return "اختر أمر شراء صالحًا قبل حفظ سند الاستلام.";
        if (field.Equals("Lines", StringComparison.OrdinalIgnoreCase))
            return code.Equals("duplicate_po_line", StringComparison.OrdinalIgnoreCase)
                ? "يوجد صنف مكرر في سند الاستلام؛ لا تُدرج السطر نفسه مرتين."
                : "أدخل كمية مستلمة أكبر من صفر لصنف واحد على الأقل، ثم أعد الحفظ.";
        if (field.Equals("ReceiptDate", StringComparison.OrdinalIgnoreCase)) return "تاريخ الاستلام غير صالح؛ اختر تاريخًا صحيحًا.";
        if (field.Equals("PostingDate", StringComparison.OrdinalIgnoreCase)) return "تاريخ الترحيل غير صالح؛ اختر تاريخًا صحيحًا.";
        if (field.EndsWith(".ReceivedQuantity", StringComparison.OrdinalIgnoreCase))
            return "الكمية المستلمة يجب أن تكون أكبر من صفر. اترك الصنف بصفر إذا لم تستلمه.";
        if (field.EndsWith(".AcceptedQuantity", StringComparison.OrdinalIgnoreCase) ||
            field.EndsWith(".RejectedQuantity", StringComparison.OrdinalIgnoreCase))
            return code.Equals("receipt_quantity_split_invalid", StringComparison.OrdinalIgnoreCase)
                ? "مجموع المقبول والمرفوض يجب أن يساوي الكمية المستلمة."
                : "أدخل كمية غير سالبة، وتأكد من تطابق مجموع المقبول والمرفوض مع المستلم.";
        if (field.EndsWith(".ActualUnitCost", StringComparison.OrdinalIgnoreCase))
            return "التكلفة الفعلية لا يمكن أن تكون سالبة.";
        if (field.EndsWith(".PurchaseOrderLineId", StringComparison.OrdinalIgnoreCase))
            return "هذا الصنف غير مرتبط بسطر صالح في أمر الشراء؛ أعد اختيار الأمر.";
        if (field.EndsWith(".LineSequence", StringComparison.OrdinalIgnoreCase))
            return "ترتيب بند الاستلام غير صحيح؛ أعد تحميل بيانات أمر الشراء.";
        return "تحقق من بيانات هذا الحقل وأعد المحاولة.";
    }
}
