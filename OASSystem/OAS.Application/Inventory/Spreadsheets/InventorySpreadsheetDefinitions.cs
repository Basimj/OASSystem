using OAS.Application.Common.Exceptions;
using OAS.Application.Spreadsheets;

namespace OAS.Application.Inventory.Spreadsheets;

public static class InventorySpreadsheetDefinitions
{
    public static readonly string[] ImportSections =
    [
        "product-categories",
        "brands",
        "product-types",
        "units",
        "products",
        "product-variants",
        "warehouses"
    ];

    private static SpreadsheetColumn Text(string key, string header, bool required = false) => new(key, required, "text", Header: header);
    private static SpreadsheetColumn Bool(string key, string header, bool required = false, string? defaultValue = null) => new(key, required, "bool", defaultValue, ["نعم", "لا"], Header: header);
    private static SpreadsheetColumn Decimal(string key, string header, bool required = false, string? defaultValue = null) => new(key, required, "decimal", defaultValue, Header: header);

    public static IReadOnlyList<SpreadsheetSheet> Get(string section) => section.Trim().ToLowerInvariant() switch
    {
        "product-categories" =>
        [
            new SpreadsheetSheet("ProductCategories",
            [
                Text("Code", "كود التصنيف", true),
                Text("Name", "الاسم", true),
                Text("ProductTypeCode", "كود نوع المنتج", true)
            ], "تصنيفات المنتجات")
        ],
        "brands" =>
        [
            new SpreadsheetSheet("Brands",
            [
                Text("Code", "كود العلامة التجارية", true),
                Text("Name", "اسم العلامة التجارية", true)
            ], "العلامات التجارية")
        ],
        "product-types" =>
        [
            new SpreadsheetSheet("ProductTypes",
            [
                Text("Code", "كود نوع المنتج", true),
                Text("Name", "الاسم", true)
            ], "أنواع المنتجات")
        ],
        "units" =>
        [
            new SpreadsheetSheet("Units",
            [
                Text("Code", "كود الوحدة", true),
                Text("Name", "الاسم", true)
            ], "الوحدات")
        ],
        "products" =>
        [
            new SpreadsheetSheet("Products",
            [
                Text("ProductCode", "كود المنتج", true),
                Text("Name", "الاسم", true),
                Text("CategoryCode", "كود التصنيف", true),
                Text("BrandCode", "كود العلامة التجارية"),
                Text("Description", "الوصف"),
                Bool("IsStockItem", "صنف مخزني", false, "نعم"),
                Text("InitialSKU", "SKU الأول"),
                Text("InitialBarcode", "باركود المتغير الأول"),
                Text("InitialVariantName", "اسم المتغير الأول"),
                Text("InitialColor", "لون المتغير الأول"),
                Text("InitialSize", "مقاس المتغير الأول"),
                Text("InitialUnitCode", "كود وحدة المتغير الأول"),
                Decimal("InitialPurchasePrice", "سعر شراء المتغير الأول"),
                Decimal("InitialSellingPrice", "سعر بيع المتغير الأول"),
                Text("FrameModel", "موديل الإطار"),
                Text("FrameMaterial", "خامة الإطار"),
                Text("FrameRimType", "نوع الإطار / Rim"),
                Text("FrameGender", "جنس الإطار"),
                Text("FrameShape", "شكل الإطار"),
                Decimal("FrameTempleLength", "طول ذراع الإطار"),
                Decimal("FrameBridgeSize", "مقاس جسر الإطار"),
                Decimal("FrameLensWidth", "عرض عدسة الإطار"),
                Text("LensType", "نوع العدسة"),
                Text("LensMaterial", "خامة العدسة"),
                Text("LensCoating", "طلاء العدسة"),
                Decimal("LensRefractiveIndex", "معامل الانكسار"),
                Decimal("LensSPH", "SPH"),
                Decimal("LensCYL", "CYL"),
                Decimal("LensADD", "ADD"),
                Decimal("LensBaseCurve", "Base Curve"),
                Decimal("LensDiameter", "Diameter"),
                Bool("LensIsPrescription", "عدسة طبية", false, "لا")
            ], "المنتجات")
        ],
        "product-variants" =>
        [
            new SpreadsheetSheet("ProductVariants",
            [
                Text("ProductCode", "كود المنتج", true),
                Text("SKU", "SKU", true),
                Text("Barcode", "الباركود"),
                Text("VariantName", "اسم المتغير"),
                Text("Color", "اللون"),
                Text("Size", "المقاس"),
                Text("UnitCode", "كود الوحدة"),
                Decimal("PurchasePrice", "سعر الشراء", true),
                Decimal("SellingPrice", "سعر البيع", true),
                Text("LensType", "نوع العدسة"),
                Text("LensMaterial", "خامة العدسة"),
                Text("LensCoating", "طلاء العدسة"),
                Decimal("LensRefractiveIndex", "معامل الانكسار"),
                Decimal("LensSPH", "SPH"),
                Decimal("LensCYL", "CYL"),
                Decimal("LensADD", "ADD"),
                Decimal("LensBaseCurve", "Base Curve"),
                Decimal("LensDiameter", "Diameter"),
                Bool("LensIsPrescription", "عدسة طبية", false, "لا")
            ], "متغيرات المنتجات")
        ],
        "warehouses" =>
        [
            new SpreadsheetSheet("Warehouses",
            [
                Text("Code", "كود المخزن", true),
                Text("Name", "الاسم", true),
                Text("Description", "الوصف"),
                Bool("IsDefault", "المخزن الافتراضي", false, "لا")
            ], "المخازن")
        ],
        _ => throw new NotFoundException("inventory spreadsheet template", section)
    };
}
