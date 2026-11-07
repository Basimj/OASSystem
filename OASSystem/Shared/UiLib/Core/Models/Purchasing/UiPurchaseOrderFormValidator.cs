namespace OAS.UiLib.Core.Models.Purchasing;

/// <summary>
/// Arabic client-side guidance for purchase orders. Server-side validators remain authoritative.
/// Keys match editor bindings, including zero-based line indexes.
/// </summary>
public static class UiPurchaseOrderFormValidator
{
    public static IReadOnlyDictionary<string, string> Validate(UiPurchaseOrderEditorModel order)
    {
        ArgumentNullException.ThrowIfNull(order);
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        void Error(string key, string message) => errors[key] = message;

        if (!order.SupplierId.HasValue || order.SupplierId == Guid.Empty)
            Error(nameof(order.SupplierId), "اختر المورد الذي ستشتري منه البضاعة.");
        if (!order.DestinationWarehouseId.HasValue || order.DestinationWarehouseId == Guid.Empty)
            Error(nameof(order.DestinationWarehouseId), "اختر المستودع الذي ستُستلم فيه البضاعة.");
        if (!order.CurrencyId.HasValue || order.CurrencyId == Guid.Empty)
            Error(nameof(order.CurrencyId), "اختر عملة أمر الشراء حتى تُحسب القيم بشكل صحيح.");
        if (order.OrderDate == default)
            Error(nameof(order.OrderDate), "حدد تاريخ أمر الشراء.");
        if (order.ExpectedDeliveryDate.HasValue && order.ExpectedDeliveryDate.Value < order.OrderDate)
            Error(nameof(order.ExpectedDeliveryDate), "موعد التوريد يجب أن يساوي تاريخ الأمر أو يأتي بعده.");
        if (order.ExchangeRate <= 0)
            Error(nameof(order.ExchangeRate), "سعر الصرف يجب أن يكون رقمًا أكبر من صفر. استخدم 1 للعملة الأساسية.");
        if (order.ExchangeRateDate == default)
            Error(nameof(order.ExchangeRateDate), "حدد تاريخ سعر الصرف.");
        if (order.TaxCalculationMode is not 1 and not 2)
            Error(nameof(order.TaxCalculationMode), "اختر هل الأسعار شاملة الضريبة أو غير شاملة لها.");
        if (order.PaymentTermDays < 0)
            Error(nameof(order.PaymentTermDays), "أجل السداد لا يمكن أن يكون سالبًا؛ أدخل صفرًا للدفع الفوري.");
        if (order.Lines.Count == 0)
            Error(nameof(order.Lines), "أضف منتجًا واحدًا على الأقل، ثم حدد وحدته وكميته وسعره.");

        for (var i = 0; i < order.Lines.Count; i++)
        {
            var line = order.Lines[i];
            var prefix = $"Lines[{i}].";
            var number = i + 1;
            if (!line.ProductVariantId.HasValue || line.ProductVariantId == Guid.Empty)
                Error(prefix + nameof(line.ProductVariantId), $"البند {number}: اختر المنتج من قائمة البحث.");
            if (!line.PurchaseUnitId.HasValue || line.PurchaseUnitId == Guid.Empty)
                Error(prefix + nameof(line.PurchaseUnitId), $"البند {number}: حدد وحدة الشراء، مثل قطعة أو كرتون.");
            if (line.OrderedQuantity <= 0)
                Error(prefix + nameof(line.OrderedQuantity), $"البند {number}: الكمية المطلوبة يجب أن تكون أكبر من صفر.");
            if (line.UnitPrice < 0)
                Error(prefix + nameof(line.UnitPrice), $"البند {number}: سعر الوحدة لا يمكن أن يكون سالبًا. يمكنك استخدام صفر في المسودة.");
            if (line.UnitConversionFactor <= 0)
                Error(prefix + nameof(line.UnitConversionFactor), $"البند {number}: معامل التحويل يجب أن يكون أكبر من صفر.");
            if (line.DiscountAmount < 0)
                Error(prefix + nameof(line.DiscountAmount), $"البند {number}: قيمة الخصم لا يمكن أن تكون سالبة.");
            else if (line.OrderedQuantity >= 0 && line.UnitPrice >= 0 &&
                Math.Round(line.DiscountAmount, 4, MidpointRounding.AwayFromZero) >
                Math.Round(line.OrderedQuantity * line.UnitPrice, 4, MidpointRounding.AwayFromZero))
                Error(prefix + nameof(line.DiscountAmount), $"البند {number}: الخصم أكبر من قيمة البند قبل الخصم. قلّله أو صحّح السعر والكمية.");
            if (line.TaxRate < 0)
                Error(prefix + nameof(line.TaxRate), $"البند {number}: نسبة الضريبة لا يمكن أن تكون سالبة.");
            if (line.ExpectedDeliveryDate.HasValue && line.ExpectedDeliveryDate.Value < order.OrderDate)
                Error(prefix + nameof(line.ExpectedDeliveryDate), $"البند {number}: موعد توريد البند لا يمكن أن يسبق تاريخ الأمر.");
            if (line.Sources.Any(x => x.AllocatedQuantity <= 0))
                Error(prefix + "Sources", $"البند {number}: الكمية المخصصة من طلب الشراء يجب أن تكون أكبر من صفر.");
        }
        return errors;
    }
}
