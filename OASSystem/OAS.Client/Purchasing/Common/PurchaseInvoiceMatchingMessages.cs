using OAS.Contracts.Common.Errors;

namespace OAS.Client.Purchasing.Common;

/// <summary>Actionable, user-facing notice for a matching rejection (never hides the server rule).</summary>
public static class PurchaseInvoiceMatchingMessages
{
    public static string FromApiError(ApiError error)
    {
        if (string.Equals(error.Code, "purchasing_match_quantity_exceeds_available_receipt", StringComparison.OrdinalIgnoreCase))
            return "تعذرت مطابقة الفاتورة: " + error.Message +
                " لم تُرحّل الفاتورة. حدّث سندات الاستلام وراجع الكميات ثم أعد تشغيل المطابقة.";
        if (string.Equals(error.Code, "purchasing_match_quantity_exceeds_invoice", StringComparison.OrdinalIgnoreCase))
            return "الكمية المطلوب مطابقتها تتجاوز كمية سطر الفاتورة. راجع كمية الفاتورة ثم أعد المحاولة.";
        if (string.Equals(error.Code, "purchasing_invoice_line_match_required", StringComparison.OrdinalIgnoreCase))
            return "يوجد بند في الفاتورة لم تُحدد له كمية استلام للمطابقة. راجع البنود وسندات الاستلام ثم أعد المحاولة.";
        return string.IsNullOrWhiteSpace(error.Message)
            ? "تعذرت المطابقة. راجع الكميات والاستلامات وحاول مرة أخرى."
            : error.Message;
    }

    public static string NotEnoughAvailable(string detail) =>
        "لا تتوفر كمية استلام غير مطابقة تكفي لهذه الفاتورة. " + detail +
        " قد تكون الكمية استُخدمت في فاتورة أخرى أو أُرجعت إلى المورد. " +
        "راجع الاستلامات أو خفّض كمية الفاتورة قبل إعادة المطابقة.";
}
