using OAS.UiLib.Core.Formatting;

namespace OAS.UiLib.Core.Models.Purchasing;

/// <summary>
/// Arabic client-side guidance for purchase orders. Server-side validators remain authoritative.
/// Keys match editor bindings, including zero-based line indexes.
/// </summary>
public static class UiPurchaseOrderFormValidator
{
    // Saving an order must validate its full landed base-unit price, not only the fields.
    // Keep Validate(..., false) for diagnostics of legacy drafts loaded from the database.
    public static IReadOnlyDictionary<string, string> ValidateForSave(UiPurchaseOrderEditorModel order) =>
        Validate(order, requirePricedLines: true);

    public static IReadOnlyDictionary<string, string> Validate(UiPurchaseOrderEditorModel order, bool requirePricedLines = false)
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
            if (!line.Id.HasValue && line.Sources.Count == 0 &&
                QuantityInputRules.UsesWholeNumbers(null, line.PurchaseUnitName) &&
                !QuantityInputRules.IsWhole(line.OrderedQuantity))
                Error(prefix + nameof(line.OrderedQuantity), $"البند {number}: {QuantityInputRules.WholeNumberMessage}");
            if (line.UnitPrice < 0)
                Error(prefix + nameof(line.UnitPrice), $"البند {number}: سعر الوحدة لا يمكن أن يكون سالبًا؛ ويجب أن يكون أكبر من صفر عند الحفظ.");
            else if (requirePricedLines && line.UnitPrice == 0m)
                Error(prefix + nameof(line.UnitPrice), $"البند {number}: أدخل سعر وحدة أكبر من صفر قبل حفظ أمر الشراء.");
            if (line.UnitConversionFactor <= 0)
                Error(prefix + nameof(line.UnitConversionFactor), $"البند {number}: معامل التحويل يجب أن يكون أكبر من صفر.");
            else if (requirePricedLines && line.OrderedQuantity > 0m &&
                Math.Round(line.OrderedQuantity * line.UnitConversionFactor, 3) <= 0m)
                Error(prefix + nameof(line.UnitConversionFactor), $"البند {number}: الكمية بالوحدة الأساسية تصبح صفرًا بعد التقريب؛ راجع الكمية ومعامل التحويل.");
            if (line.DiscountAmount < 0)
                Error(prefix + nameof(line.DiscountAmount), $"البند {number}: قيمة الخصم لا يمكن أن تكون سالبة.");
            else if (line.OrderedQuantity >= 0 && line.UnitPrice >= 0 &&
                Math.Round(line.DiscountAmount, 4, MidpointRounding.AwayFromZero) >
                Math.Round(line.OrderedQuantity * line.UnitPrice, 4, MidpointRounding.AwayFromZero))
                Error(prefix + nameof(line.DiscountAmount), $"البند {number}: الخصم أكبر من قيمة البند قبل الخصم. قلّله أو صحّح السعر والكمية.");
            if (line.TaxRate < 0)
                Error(prefix + nameof(line.TaxRate), $"البند {number}: نسبة الضريبة لا يمكن أن تكون سالبة.");
            if (requirePricedLines && line.UnitPrice > 0m && line.OrderedQuantity > 0m &&
                line.UnitConversionFactor > 0m && order.ExchangeRate > 0m &&
                line.DiscountAmount >= 0m && line.TaxRate >= 0m &&
                order.TaxCalculationMode is 1 or 2)
            {
                // Mirror the four-decimal line valuation used by the purchase-order domain
                // and the base inventory-unit cost displayed on purchase receipts.
                var gross = Math.Round(line.OrderedQuantity * line.UnitPrice, 4, MidpointRounding.AwayFromZero);
                var discount = Math.Round(line.DiscountAmount, 4, MidpointRounding.AwayFromZero);
                if (gross > discount)
                {
                    var priceAfterDiscount = Math.Round(gross - discount, 4, MidpointRounding.AwayFromZero);
                    var inclusiveTax = order.TaxCalculationMode == 2 && line.TaxRate > 0m
                        ? Math.Round(priceAfterDiscount - priceAfterDiscount / (1m + line.TaxRate / 100m),
                            4, MidpointRounding.AwayFromZero)
                        : 0m;
                    var net = Math.Round(priceAfterDiscount - inclusiveTax, 4, MidpointRounding.AwayFromZero);
                    if (net <= 0m)
                        Error(prefix + nameof(line.DiscountAmount), $"البند {number}: صافي القيمة بعد الخصم والضريبة يجب أن يكون أكبر من صفر.");
                    else if (Math.Round((net / line.OrderedQuantity * order.ExchangeRate) / line.UnitConversionFactor, 4) <= 0m)
                        Error(prefix + nameof(line.UnitPrice), $"البند {number}: تكلفة الوحدة الأساسية تُقرب إلى صفر؛ راجع السعر والخصم وسعر الصرف ومعامل التحويل.");
                }
                else if (gross == discount)
                    Error(prefix + nameof(line.DiscountAmount), $"البند {number}: الخصم يساوي كامل قيمة البند؛ يجب بقاء صافي تكلفة موجب قبل الحفظ.");
            }
            if (line.ExpectedDeliveryDate.HasValue && line.ExpectedDeliveryDate.Value < order.OrderDate)
                Error(prefix + nameof(line.ExpectedDeliveryDate), $"البند {number}: موعد توريد البند لا يمكن أن يسبق تاريخ الأمر.");
            if (line.Sources.Any(x => x.AllocatedQuantity <= 0))
                Error(prefix + "Sources", $"البند {number}: الكمية المخصصة من طلب الشراء يجب أن تكون أكبر من صفر.");
        }
        return errors;
    }
}
