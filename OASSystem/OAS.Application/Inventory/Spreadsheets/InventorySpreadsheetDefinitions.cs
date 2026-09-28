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

    private static SpreadsheetColumn Text(string key, string header, bool required = false) =>
        new(key, required, "text", Header: header);

    private static SpreadsheetColumn Bool(string key, string header, bool required = false, string? defaultValue = null) =>
        new(key, required, "bool", defaultValue, ["نعم", "لا"], Header: header);

    private static SpreadsheetColumn Decimal(string key, string header, bool required = false, string? defaultValue = null) =>
        new(key, required, "decimal", defaultValue, Header: header);

    public static IReadOnlyList<SpreadsheetSheet> Get(string section) => section.Trim().ToLowerInvariant() switch
    {
        "product-categories" =>
        [
            new SpreadsheetSheet("ProductCategories",
            [
                Text("Code", "كود التصنيف", true),
                Text("NameAr", "الاسم العربي", true),
                Text("NameEn", "الاسم الإنجليزي"),
                Text("ParentCode", "كود التصنيف الأب")
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
                Text("NameAr", "الاسم العربي", true),
                Text("NameEn", "الاسم الإنجليزي")
            ], "أنواع المنتجات")
        ],

        "units" =>
        [
            new SpreadsheetSheet("Units",
            [
                Text("Code", "كود الوحدة", true),
                Text("NameAr", "الاسم العربي", true),
                Text("NameEn", "الاسم الإنجليزي")
            ], "الوحدات")
        ],

        "products" =>
        [
            new SpreadsheetSheet("Products",
            [
                Text("ProductCode", "كود المنتج", true),
                Text("NameAr", "الاسم العربي", true),
                Text("NameEn", "الاسم الإنجليزي"),
                Text("CategoryCode", "كود التصنيف", true),
                Text("BrandCode", "كود العلامة التجارية"),
                Text("ProductTypeCode", "كود نوع المنتج", true),
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
                Decimal("LensSphereMin", "Sphere Min"),
                Decimal("LensSphereMax", "Sphere Max"),
                Decimal("LensCylinderMin", "Cylinder Min"),
                Decimal("LensCylinderMax", "Cylinder Max"),
                Decimal("LensAddMin", "Add Min"),
                Decimal("LensAddMax", "Add Max"),
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
                Decimal("SellingPrice", "سعر البيع", true)
            ], "متغيرات المنتجات")
        ],

        "warehouses" =>
        [
            new SpreadsheetSheet("Warehouses",
            [
                Text("Code", "كود المخزن", true),
                Text("NameAr", "الاسم العربي", true),
                Text("NameEn", "الاسم الإنجليزي"),
                Text("Description", "الوصف"),
                Bool("IsDefault", "المخزن الافتراضي", false, "لا")
            ], "المخازن")
        ],

        _ => throw new NotFoundException("inventory spreadsheet template", section)
    };
}
