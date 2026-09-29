using OAS.Contracts.Common.Errors;

namespace OAS.Client.Sales.Common;

public static class SalesApiErrorPresenter
{
    private static readonly IReadOnlyDictionary<string, string> Messages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["sales_cost_snapshot_missing"] = "تعذر الحصول على تكلفة المخزون الفعلية لأحد أسطر الفاتورة.",
        ["sales_credit_limit_exceeded"] = "العملية تتجاوز الحد الائتماني المسموح للعميل.",
        ["sales_credit_not_allowed"] = "البيع الآجل غير مسموح لهذا العميل.",
        ["sales_customer_account_invalid"] = "الحساب المحاسبي المرتبط بالعميل غير صالح للترحيل.",
        ["sales_customer_inactive"] = "العميل المحدد غير نشط.",
        ["sales_duplicate_invoice_code"] = "كود الفاتورة مستخدم مسبقًا.",
        ["sales_duplicate_order_code"] = "كود طلب العميل مستخدم مسبقًا.",
        ["sales_duplicate_prescription_code"] = "كود الوصفة مستخدم مسبقًا.",
        ["sales_insufficient_stock"] = "الكمية المتاحة غير كافية لإتمام العملية.",
        ["sales_invalid_prescription_revision"] = "إصدار الوصفة المحدد غير صالح.",
        ["sales_invoice_already_posted"] = "تم ترحيل الفاتورة مسبقًا.",
        ["sales_invoice_delete_forbidden"] = "لا يمكن حذف فاتورة المبيعات في حالتها الحالية.",
        ["sales_invoice_id_required"] = "معرف فاتورة المبيعات مطلوب.",
        ["sales_invoice_invalid_status"] = "حالة الفاتورة الحالية لا تسمح بهذه العملية.",
        ["sales_invoice_line_has_price_override_history"] = "لا يمكن تغيير هذا السطر بصمت لأن له سجل اعتماد سعر سابق.",
        ["sales_invoice_lines_required"] = "يجب أن تحتوي الفاتورة على سطر بيع واحد على الأقل.",
        ["sales_invoice_not_found"] = "فاتورة المبيعات غير موجودة.",
        ["sales_invoice_period_closed"] = "الفترة المالية المحددة مغلقة أو مقفلة للمحاسبة.",
        ["sales_invoice_period_not_found"] = "لا توجد فترة مالية تغطي تاريخ الترحيل المحدد.",
        ["sales_order_delete_forbidden"] = "لا يمكن حذف طلب العميل في حالته الحالية.",
        ["sales_order_id_required"] = "معرف طلب العميل مطلوب.",
        ["sales_order_invalid_status"] = "حالة طلب العميل الحالية لا تسمح بهذه العملية.",
        ["sales_order_lines_required"] = "يجب أن يحتوي طلب العميل على سطر واحد على الأقل.",
        ["sales_payment_allocation_exceeds_outstanding"] = "مبلغ التخصيص يتجاوز الرصيد المتبقي على الفاتورة.",
        ["sales_payment_allocation_invoice_not_posted"] = "لا يمكن تخصيص دفعة إلا لفاتورة مبيعات مرحّلة.",
        ["sales_posting_account_invalid"] = "أحد الحسابات المستخدمة في ترحيل المبيعات غير صالح.",
        ["sales_posting_profile_missing"] = "لا يوجد Posting Profile فعال لفاتورة المبيعات.",
        ["sales_posting_role_missing"] = "Posting Profile للمبيعات يفتقد حسابًا مطلوبًا للترحيل.",
        ["sales_prescription_delete_forbidden"] = "لا يمكن حذف الوصفة في حالتها الحالية.",
        ["sales_prescription_eye_duplicate"] = "يوجد قياس مكرر لنفس العين داخل Revision الوصفة.",
        ["sales_prescription_eye_required"] = "بيانات العين المطلوبة غير مكتملة.",
        ["sales_prescription_id_required"] = "معرف الوصفة مطلوب.",
        ["sales_prescription_required"] = "يجب تحديد وصفة وإصدار صالح لهذا السطر.",
        ["sales_prescription_status_invalid"] = "حالة الوصفة الحالية لا تسمح بهذه العملية.",
        ["sales_price_override_id_required"] = "معرف طلب اعتماد السعر مطلوب.",
        ["sales_price_override_not_approved"] = "تغيير السعر لم يعتمد بعد.",
        ["sales_price_override_pending"] = "يوجد طلب اعتماد سعر معلق لهذا السطر.",
        ["sales_price_override_required"] = "السعر المعدل يحتاج طلب اعتماد.",
        ["sales_product_inactive"] = "المنتج المحدد غير نشط.",
        ["sales_product_not_stock_item"] = "المنتج المحدد ليس صنفًا مخزنيًا صالحًا لهذا النوع من أسطر البيع.",
        ["sales_product_required"] = "يجب تحديد المنتج لهذا السطر.",
        ["sales_product_variant_inactive"] = "متغير المنتج المحدد غير نشط.",
        ["sales_reservation_conflict"] = "الكمية المتاحة تغيرت أثناء تنفيذ العملية. حدّث المستند وحاول مرة أخرى.",
        ["sales_warehouse_inactive"] = "المخزن المحدد غير نشط.",
        ["sales_warehouse_required"] = "يجب تحديد المخزن لهذا السطر.",
        ["concurrency_conflict"] = "تم تعديل السجل بواسطة مستخدم آخر. حدّث البيانات وحاول مرة أخرى."
    };

    public static string GetMessage(ApiError? error, string fallback = "تعذر إتمام العملية.")
        => error is not null && Messages.TryGetValue(error.Code ?? string.Empty, out var message)
            ? message
            : !string.IsNullOrWhiteSpace(error?.Message) ? error.Message : fallback;
}
