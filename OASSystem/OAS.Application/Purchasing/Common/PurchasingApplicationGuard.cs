using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;

namespace OAS.Application.Purchasing.Common;

internal static class PurchasingApplicationGuard
{
    public static void Supplier(PurchasingSupplierSnapshot? supplier)
    {
        if (supplier is null) throw new RequestValidationException(new Dictionary<string, string[]> { ["SupplierId"] = ["supplier_not_found"] });
        if (!supplier.IsActive) throw new ConflictException("purchasing_supplier_inactive", "المورد غير فعال.");
    }

    public static void Warehouse(PurchasingWarehouseSnapshot? warehouse)
    {
        if (warehouse is null) throw new RequestValidationException(new Dictionary<string, string[]> { ["WarehouseId"] = ["warehouse_not_found"] });
        if (!warehouse.IsActive) throw new ConflictException("purchasing_warehouse_inactive", "المخزن غير فعال.");
    }

    public static void Currency(PurchasingCurrencySnapshot? currency)
    {
        if (currency is null) throw new RequestValidationException(new Dictionary<string, string[]> { ["CurrencyId"] = ["currency_not_found"] });
        if (!currency.IsActive) throw new ConflictException("purchasing_currency_inactive", "العملة غير فعالة.");
    }

    public static void Unit(PurchasingUnitSnapshot? unit)
    {
        if (unit is null) throw new RequestValidationException(new Dictionary<string, string[]> { ["PurchaseUnitId"] = ["unit_not_found"] });
        if (!unit.IsActive) throw new ConflictException("purchasing_unit_inactive", "وحدة الشراء غير فعالة.");
    }

    public static void Product(PurchasingProductSnapshot? product)
    {
        if (product is null) throw new RequestValidationException(new Dictionary<string, string[]> { ["ProductVariantId"] = ["product_variant_not_found"] });
        if (!product.VariantIsActive || !product.ProductIsActive) throw new ConflictException("purchasing_product_inactive", "المنتج غير فعال.");
        if (!product.IsStockItem) throw new ConflictException("purchasing_product_not_stock_item", "لا يمكن استخدام منتج غير مخزني في دورة المشتريات الحالية.");
    }
}
