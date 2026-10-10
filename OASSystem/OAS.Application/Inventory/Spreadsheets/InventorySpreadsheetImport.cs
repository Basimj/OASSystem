using System.Globalization;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.CRUD.Abstractions;
using OAS.Application.Inventory.Authorization;
using OAS.Application.Inventory.Products.Products.Commands.CreateStockProduct;
using OAS.Application.Spreadsheets;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Products;
using OAS.Contracts.Inventory.Warehouses;
using OAS.Contracts.Spreadsheets;

namespace OAS.Application.Inventory.Spreadsheets;

public sealed partial class InventorySpreadsheetService
{
    private sealed class ImportRow(SpreadsheetRow source)
    {
        public SpreadsheetRow Source { get; } = source;
        public List<string> Errors { get; } = [];
        public List<string> Warnings { get; } = [];

        public string Get(string key) => Source.Values.GetValueOrDefault(key)?.Trim() ?? string.Empty;
        public string? Optional(string key) => Get(key) is { Length: > 0 } value ? value : null;

        public bool Bool(string key, bool fallback = false)
        {
            var value = Get(key);
            if (value.Length == 0) return fallback;
            return value.Trim().ToLowerInvariant() switch
            {
                "نعم" or "yes" or "true" or "1" => true,
                "لا" or "no" or "false" or "0" => false,
                _ => throw new FormatException($"{key}: قيمة منطقية غير صالحة.")
            };
        }

        public decimal Decimal(string key) =>
            decimal.Parse(Get(key), NumberStyles.Number, CultureInfo.InvariantCulture);

        public decimal? NullableDecimal(string key)
        {
            var value = Optional(key);
            return value is null ? null : decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
        }

        public SpreadsheetRowResult Result() =>
            new(Source.Sheet, Source.Number, Source.Values, Errors, Warnings);
    }

    private sealed record ImportPlan(string Section, IReadOnlyList<ImportRow> Rows);

    public async Task<byte[]> TemplateAsync(string section, CancellationToken cancellationToken = default)
    {
        section = NormalizeImportSection(section);
        await AuthorizeImportAsync(section, cancellationToken);
        var tables = InventorySpreadsheetDefinitions.Get(section)
            .Select(definition => new SpreadsheetTable(definition, []))
            .ToArray();
        return Workbook.Write(tables, template: true);
    }

    public async Task<SpreadsheetPreview> PreviewAsync(
        string section,
        byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        section = NormalizeImportSection(section);
        await AuthorizeImportAsync(section, cancellationToken);

        try
        {
            var plan = await PrepareImportAsync(section, bytes, cancellationToken);
            return new SpreadsheetPreview(plan.Rows.Select(x => x.Result()).ToArray());
        }
        catch (RequestValidationException ex)
        {
            return InventoryFileValidationPreview(
                ex.Errors.SelectMany(x => x.Value).FirstOrDefault() ?? "ملف Excel غير صالح.");
        }
        catch (Exception ex) when (IsSpreadsheetReadException(ex))
        {
            return InventoryFileValidationPreview("تعذر قراءة ملف Excel. استخدم قالب Excel الخاص بهذه الشاشة.");
        }
    }

    public async Task<SpreadsheetPreview> ImportAsync(
        string section,
        byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        section = NormalizeImportSection(section);
        await AuthorizeImportAsync(section, cancellationToken);

        var unitOfWork = Services.GetRequiredService<IUnitOfWork>();

        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                var plan = await PrepareImportAsync(section, bytes, token);
                var preview = new SpreadsheetPreview(plan.Rows.Select(x => x.Result()).ToArray());
                if (!preview.CanImport)
                    return preview;

                var imported = await ExecuteImportAsync(plan, token);
                return preview with { ImportedRecords = imported };
            }, cancellationToken);
        }
        catch (RequestValidationException ex)
        {
            return InventoryFileValidationPreview(
                ex.Errors.SelectMany(x => x.Value).FirstOrDefault() ?? "ملف Excel غير صالح.");
        }
        catch (ConflictException)
        {
            return InventoryFileValidationPreview("تغيّرت البيانات منذ الفحص أو يوجد كود/رقم مكرر. أعد فحص الملف ثم حاول مرة أخرى.");
        }
        catch (Exception ex) when (IsSpreadsheetReadException(ex))
        {
            return InventoryFileValidationPreview("تعذر قراءة ملف Excel. استخدم قالب Excel الخاص بهذه الشاشة.");
        }
    }

    private static string NormalizeImportSection(string section)
    {
        section = section.Trim().ToLowerInvariant();
        if (!InventorySpreadsheetDefinitions.ImportSections.Contains(section, StringComparer.Ordinal))
            throw new NotFoundException("inventory spreadsheet", section);
        return section;
    }

    private async Task AuthorizeImportAsync(string section, CancellationToken cancellationToken)
    {
        var permission = section switch
        {
            "product-categories" => InventoryPermissions.ProductCategories.Create,
            "brands" => InventoryPermissions.Brands.Create,
            "product-types" => InventoryPermissions.Products.Create,
            "units" => InventoryPermissions.Units.Create,
            "products" => InventoryPermissions.Products.Create,
            "product-variants" => InventoryPermissions.ProductVariants.Create,
            "warehouses" => InventoryPermissions.Warehouses.Create,
            _ => throw new NotFoundException("inventory spreadsheet", section)
        };

        if (!await Permissions.HasPermissionAsync(permission, cancellationToken))
            throw new ForbiddenException();
    }

    private async Task<ImportPlan> PrepareImportAsync(
        string section,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        var definitions = InventorySpreadsheetDefinitions.Get(section);
        var rows = Workbook.Read(bytes, definitions).Select(x => new ImportRow(x)).ToList();

        ValidateColumnTypes(definitions, rows);

        var categories = await CategoriesAsync(new PageRequest(), cancellationToken);
        var brands = await BrandsAsync(new PageRequest(), cancellationToken);
        var productTypes = await ProductTypesAsync(new PageRequest(), cancellationToken);
        var units = await UnitsAsync(new PageRequest(), cancellationToken);
        var products = await ProductsAsync(new PageRequest(), cancellationToken);
        var variants = await VariantsAsync(new PageRequest(), cancellationToken);
        var warehouses = await WarehousesAsync(new PageRequest(), cancellationToken);

        switch (section)
        {
            case "product-categories":
                ValidateCategories(rows, categories);
                break;
            case "brands":
                ValidateBrands(rows, brands);
                break;
            case "product-types":
                ValidateProductTypes(rows, productTypes);
                break;
            case "units":
                ValidateUnits(rows, units);
                break;
            case "products":
                ValidateProducts(rows, products, variants, categories, brands, productTypes, units);
                break;
            case "product-variants":
                ValidateVariants(rows, variants, products, units);
                break;
            case "warehouses":
                ValidateWarehouses(rows, warehouses);
                break;
        }

        return new ImportPlan(section, rows);
    }

    private static void ValidateColumnTypes(
        IReadOnlyList<SpreadsheetSheet> definitions,
        IReadOnlyList<ImportRow> rows)
    {
        foreach (var row in rows)
        {
            var definition = definitions.Single(x => x.Name == row.Source.Sheet);
            foreach (var column in definition.Columns)
            {
                var header = column.Header ?? column.Key;
                var value = row.Get(column.Key);
                if (value.Length == 0)
                {
                    if (column.Required && string.IsNullOrWhiteSpace(column.DefaultValue))
                        row.Errors.Add($"{header}: مطلوب.");
                    continue;
                }

                if (column.DataType == "bool" &&
                    value.Trim().ToLowerInvariant() is not ("نعم" or "لا" or "yes" or "no" or "true" or "false" or "1" or "0"))
                {
                    row.Errors.Add($"{header}: استخدم نعم أو لا.");
                }

                if (column.DataType == "decimal")
                {
                    if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                    {
                        row.Errors.Add($"{header}: رقم غير صالح.");
                    }
                    else
                    {
                        var allowsNegative = column.Key is
                            "LensSphereMin" or "LensSphereMax" or
                            "LensCylinderMin" or "LensCylinderMax";

                        if ((!allowsNegative && number < 0) || decimal.Round(number, 4) != number)
                        {
                            row.Errors.Add(allowsNegative
                                ? $"{header}: استخدم رقماً بحد أقصى 4 منازل عشرية."
                                : $"{header}: استخدم رقماً غير سالب وبحد أقصى 4 منازل عشرية.");
                        }
                    }
                }
            }
        }
    }

    private static void ValidateCategories(IReadOnlyList<ImportRow> rows, IReadOnlyList<ProductCategoryDto> existing)
    {
        ValidateCodeRows(rows, "Code", "كود التصنيف", existing.Select(x => x.Code), 32);
        foreach (var row in rows)
        {
            ValidateRequiredText(row, "NameAr", "الاسم العربي", 100);
            ValidateOptionalText(row, "NameEn", "الاسم الإنجليزي", 100);
            var parentCode = row.Optional("ParentCode");
            if (parentCode is null) continue;
            if (string.Equals(parentCode, row.Get("Code"), StringComparison.OrdinalIgnoreCase))
                row.Errors.Add("كود التصنيف الأب: لا يمكن أن يكون التصنيف أبًا لنفسه.");
            else if (!existing.Any(x => Eq(x.Code, parentCode)) && !rows.Any(x => Eq(x.Get("Code"), parentCode)))
                row.Errors.Add("كود التصنيف الأب: الكود غير موجود في النظام أو الملف.");
        }

        var parentByCode = rows
            .Where(x => x.Get("Code").Length > 0)
            .GroupBy(x => x.Get("Code"), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Optional("ParentCode"), StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var start = row.Get("Code");
            if (start.Length == 0) continue;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { start };
            var current = row.Optional("ParentCode");
            while (current is not null && parentByCode.TryGetValue(current, out var next))
            {
                if (!seen.Add(current))
                {
                    row.Errors.Add("كود التصنيف الأب: توجد دورة غير صالحة في شجرة التصنيفات.");
                    break;
                }
                current = next;
            }
        }
    }

    private static void ValidateBrands(IReadOnlyList<ImportRow> rows, IReadOnlyList<BrandDto> existing)
    {
        ValidateCodeRows(rows, "Code", "كود العلامة التجارية", existing.Select(x => x.Code), 32);
        foreach (var row in rows)
            ValidateRequiredText(row, "Name", "اسم العلامة التجارية", 100);
    }

    private static void ValidateProductTypes(IReadOnlyList<ImportRow> rows, IReadOnlyList<ProductTypeDto> existing)
    {
        ValidateCodeRows(rows, "Code", "كود نوع المنتج", existing.Select(x => x.Code), 32);
        foreach (var row in rows)
        {
            ValidateRequiredText(row, "NameAr", "الاسم العربي", 100);
            ValidateOptionalText(row, "NameEn", "الاسم الإنجليزي", 100);
        }
    }

    private static void ValidateUnits(IReadOnlyList<ImportRow> rows, IReadOnlyList<UnitDto> existing)
    {
        ValidateCodeRows(rows, "Code", "كود الوحدة", existing.Select(x => x.Code), 32);
        foreach (var row in rows)
        {
            ValidateRequiredText(row, "NameAr", "الاسم العربي", 100);
            ValidateOptionalText(row, "NameEn", "الاسم الإنجليزي", 100);
        }
    }

    private static void ValidateProducts(
        IReadOnlyList<ImportRow> rows,
        IReadOnlyList<ProductDto> existing,
        IReadOnlyList<ProductVariantDto> existingVariants,
        IReadOnlyList<ProductCategoryDto> categories,
        IReadOnlyList<BrandDto> brands,
        IReadOnlyList<ProductTypeDto> productTypes,
        IReadOnlyList<UnitDto> units)
    {
        ValidateCodeRows(rows, "ProductCode", "كود المنتج", existing.Select(x => x.ProductCode), 32);
        var existingSkus = new HashSet<string>(existingVariants.Select(x => x.SKU), StringComparer.OrdinalIgnoreCase);
        var existingBarcodes = new HashSet<string>(existingVariants.Where(x => !string.IsNullOrWhiteSpace(x.Barcode)).Select(x => x.Barcode!), StringComparer.OrdinalIgnoreCase);
        var fileSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fileBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            ValidateRequiredText(row, "NameAr", "الاسم العربي", 150);
            ValidateOptionalText(row, "NameEn", "الاسم الإنجليزي", 150);
            ValidateOptionalText(row, "Description", "الوصف", 500);
            ValidateOptionalText(row, "InitialSKU", "SKU الأول", 64);
            ValidateOptionalText(row, "InitialBarcode", "باركود المتغير الأول", 64);
            ValidateOptionalText(row, "InitialVariantName", "اسم المتغير الأول", 100);
            ValidateOptionalText(row, "InitialColor", "لون المتغير الأول", 50);
            ValidateOptionalText(row, "InitialSize", "مقاس المتغير الأول", 50);
            ValidateOptionalText(row, "FrameModel", "موديل الإطار", 100);
            ValidateOptionalText(row, "FrameMaterial", "خامة الإطار", 50);
            ValidateOptionalText(row, "FrameRimType", "نوع الإطار / Rim", 50);
            ValidateOptionalText(row, "FrameGender", "جنس الإطار", 20);
            ValidateOptionalText(row, "FrameShape", "شكل الإطار", 50);
            ValidateOptionalText(row, "LensType", "نوع العدسة", 50);
            ValidateOptionalText(row, "LensMaterial", "خامة العدسة", 50);
            ValidateOptionalText(row, "LensCoating", "طلاء العدسة", 50);

            var categoryCode = row.Get("CategoryCode");
            var category = categories.FirstOrDefault(x => Eq(x.Code, categoryCode));
            if (categoryCode.Length > 0 && category is null)
                row.Errors.Add("كود التصنيف: الكود غير موجود.");
            else if (category is { IsActive: false })
                row.Errors.Add("كود التصنيف: التصنيف غير نشط.");

            var brandCode = row.Optional("BrandCode");
            var brand = brandCode is null ? null : brands.FirstOrDefault(x => Eq(x.Code, brandCode));
            if (brandCode is not null && brand is null)
                row.Errors.Add("كود العلامة التجارية: الكود غير موجود.");
            else if (brand is { IsActive: false })
                row.Errors.Add("كود العلامة التجارية: العلامة التجارية غير نشطة.");

            var productTypeCode = row.Get("ProductTypeCode");
            var productType = productTypes.FirstOrDefault(x => Eq(x.Code, productTypeCode));
            if (productTypeCode.Length > 0 && productType is null)
                row.Errors.Add("كود نوع المنتج: الكود غير موجود.");
            else if (productType is { IsActive: false })
                row.Errors.Add("كود نوع المنتج: نوع المنتج غير نشط.");

            var isStock = TryBool(row.Get("IsStockItem"), true);
            if (productType is not null &&
                string.Equals(productType.SystemKey, ProductTypeSystemKeys.Service, StringComparison.OrdinalIgnoreCase) &&
                isStock)
            {
                row.Errors.Add("صنف مخزني: نوع الخدمة لا يمكن أن يكون صنفًا مخزنيًا.");
            }

            var hasFrameDetails = HasAny(row,
                "FrameModel", "FrameMaterial", "FrameRimType", "FrameGender", "FrameShape",
                "FrameTempleLength", "FrameBridgeSize", "FrameLensWidth");
            var hasLensDetails = HasAny(row,
                "LensType", "LensMaterial", "LensCoating", "LensRefractiveIndex",
                "LensSphereMin", "LensSphereMax", "LensCylinderMin", "LensCylinderMax",
                "LensAddMin", "LensAddMax") || TryBool(row.Get("LensIsPrescription"), false);

            if (hasFrameDetails)
            {
                var supportsFrameDetails = productType is not null &&
                    (string.Equals(productType.SystemKey, ProductTypeSystemKeys.Frame, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(productType.SystemKey, ProductTypeSystemKeys.Sunglasses, StringComparison.OrdinalIgnoreCase));
                if (!supportsFrameDetails)
                    row.Errors.Add("موديل الإطار: تفاصيل الإطار مسموحة فقط لنوع إطار أو نظارة شمسية.");
                if (row.Optional("FrameModel") is null)
                    row.Errors.Add("موديل الإطار: مطلوب عند إدخال تفاصيل الإطار.");
                ValidateDecimalPrecision(row, "FrameTempleLength", "طول ذراع الإطار", 6, 2, allowNegative: false);
                ValidateDecimalPrecision(row, "FrameBridgeSize", "مقاس جسر الإطار", 6, 2, allowNegative: false);
                ValidateDecimalPrecision(row, "FrameLensWidth", "عرض عدسة الإطار", 6, 2, allowNegative: false);
            }

            if (hasLensDetails)
            {
                if (!string.Equals(productType?.SystemKey, ProductTypeSystemKeys.Lens, StringComparison.OrdinalIgnoreCase))
                    row.Errors.Add("نوع العدسة: تفاصيل العدسة مسموحة فقط لنوع منتج عدسة.");
                if (row.Optional("LensType") is null)
                    row.Errors.Add("نوع العدسة: مطلوب عند إدخال تفاصيل العدسة.");
                ValidateDecimalPrecision(row, "LensRefractiveIndex", "معامل الانكسار", 5, 3, allowNegative: false);
                var refractiveIndex = row.NullableDecimal("LensRefractiveIndex");
                if (refractiveIndex.HasValue && refractiveIndex.Value <= 0m)
                    row.Errors.Add("معامل الانكسار: يجب أن يكون أكبر من صفر عند إدخاله.");
                ValidateDecimalPrecision(row, "LensSphereMin", "Sphere Min", 6, 2, allowNegative: true);
                ValidateDecimalPrecision(row, "LensSphereMax", "Sphere Max", 6, 2, allowNegative: true);
                ValidateDecimalPrecision(row, "LensCylinderMin", "Cylinder Min", 6, 2, allowNegative: true);
                ValidateDecimalPrecision(row, "LensCylinderMax", "Cylinder Max", 6, 2, allowNegative: true);
                ValidateDecimalPrecision(row, "LensAddMin", "Add Min", 6, 2, allowNegative: false);
                ValidateDecimalPrecision(row, "LensAddMax", "Add Max", 6, 2, allowNegative: false);
            }

            var sku = row.Optional("InitialSKU");
            var hasInitialVariantData = HasAny(row,
                "InitialBarcode", "InitialVariantName", "InitialColor", "InitialSize",
                "InitialUnitCode", "InitialPurchasePrice", "InitialSellingPrice");
            if ((isStock || hasInitialVariantData) && sku is null)
                row.Errors.Add(isStock
                    ? "SKU الأول: مطلوب للمنتج المخزني."
                    : "SKU الأول: مطلوب عند إدخال بيانات المتغير الأول.");

            if (sku is not null)
            {
                if (existingSkus.Contains(sku))
                    row.Errors.Add("SKU الأول: مستخدم مسبقًا.");
                else if (!fileSkus.Add(sku))
                    row.Errors.Add("SKU الأول: مكرر داخل الملف.");
            }

            var barcode = row.Optional("InitialBarcode");
            if (barcode is not null)
            {
                if (existingBarcodes.Contains(barcode))
                    row.Errors.Add("باركود المتغير الأول: مستخدم مسبقًا.");
                else if (!fileBarcodes.Add(barcode))
                    row.Errors.Add("باركود المتغير الأول: مكرر داخل الملف.");
            }

            var unitCode = row.Optional("InitialUnitCode");
            var unit = unitCode is null ? null : units.FirstOrDefault(x => Eq(x.Code, unitCode));
            if (unitCode is not null && unit is null)
                row.Errors.Add("كود وحدة المتغير الأول: الكود غير موجود.");
            else if (unit is { IsActive: false })
                row.Errors.Add("كود وحدة المتغير الأول: الوحدة غير نشطة.");

            if (sku is not null || isStock || hasInitialVariantData)
            {
                ValidateRequiredDecimal(row, "InitialPurchasePrice", "سعر شراء المتغير الأول", 2);
                ValidateRequiredDecimal(row, "InitialSellingPrice", "سعر بيع المتغير الأول", 2);
                ValidateDecimalPrecision(row, "InitialPurchasePrice", "سعر شراء المتغير الأول", 18, 2, allowNegative: false);
                ValidateDecimalPrecision(row, "InitialSellingPrice", "سعر بيع المتغير الأول", 18, 2, allowNegative: false);
            }
        }
    }

    private static void ValidateVariants(
        IReadOnlyList<ImportRow> rows,
        IReadOnlyList<ProductVariantDto> existing,
        IReadOnlyList<ProductDto> products,
        IReadOnlyList<UnitDto> units)
    {
        ValidateCodeRows(rows, "SKU", "SKU", existing.Select(x => x.SKU), 64);
        var existingBarcodes = existing.Where(x => !string.IsNullOrWhiteSpace(x.Barcode)).Select(x => x.Barcode!);
        var fileBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var productCode = row.Get("ProductCode");
            var product = products.FirstOrDefault(x => Eq(x.ProductCode, productCode));
            if (productCode.Length > 0 && product is null)
                row.Errors.Add("كود المنتج: الكود غير موجود.");
            else if (product is { IsActive: false })
                row.Errors.Add("كود المنتج: المنتج غير نشط.");

            ValidateOptionalText(row, "Barcode", "الباركود", 64);
            ValidateOptionalText(row, "VariantName", "اسم المتغير", 100);
            ValidateOptionalText(row, "Color", "اللون", 50);
            ValidateOptionalText(row, "Size", "المقاس", 50);

            var unitCode = row.Optional("UnitCode");
            var unit = unitCode is null ? null : units.FirstOrDefault(x => Eq(x.Code, unitCode));
            if (unitCode is not null && unit is null)
                row.Errors.Add("كود الوحدة: الكود غير موجود.");
            else if (unit is { IsActive: false })
                row.Errors.Add("كود الوحدة: الوحدة غير نشطة.");

            ValidateRequiredDecimal(row, "PurchasePrice", "سعر الشراء", 2);
            ValidateRequiredDecimal(row, "SellingPrice", "سعر البيع", 2);
            ValidateDecimalPrecision(row, "PurchasePrice", "سعر الشراء", 18, 2, allowNegative: false);
            ValidateDecimalPrecision(row, "SellingPrice", "سعر البيع", 18, 2, allowNegative: false);

            var barcode = row.Optional("Barcode");
            if (barcode is not null)
            {
                if (existingBarcodes.Any(x => Eq(x, barcode)))
                    row.Errors.Add("الباركود: مستخدم مسبقًا.");
                else if (!fileBarcodes.Add(barcode))
                    row.Errors.Add("الباركود: مكرر داخل الملف.");
            }
        }
    }

    private static void ValidateWarehouses(IReadOnlyList<ImportRow> rows, IReadOnlyList<WarehouseDto> existing)
    {
        ValidateCodeRows(rows, "Code", "كود المخزن", existing.Select(x => x.Code), 32);
        foreach (var row in rows)
        {
            ValidateRequiredText(row, "NameAr", "الاسم العربي", 100);
            ValidateOptionalText(row, "NameEn", "الاسم الإنجليزي", 100);
            ValidateOptionalText(row, "Description", "الوصف", 500);
        }

        var requestedDefaults = rows.Count(x => TryBool(x.Get("IsDefault"), false));
        if (requestedDefaults > 0 && existing.Any(x => x.IsDefault && x.IsActive))
        {
            foreach (var row in rows.Where(x => TryBool(x.Get("IsDefault"), false)))
                row.Errors.Add("المخزن الافتراضي: يوجد مخزن افتراضي فعال بالفعل.");
        }
        else if (requestedDefaults > 1)
        {
            foreach (var row in rows.Where(x => TryBool(x.Get("IsDefault"), false)))
                row.Errors.Add("المخزن الافتراضي: لا يمكن تعيين أكثر من مخزن افتراضي واحد في الملف.");
        }
    }

    private async Task<int> ExecuteImportAsync(ImportPlan plan, CancellationToken cancellationToken)
    {
        switch (plan.Section)
        {
            case "product-categories":
                return await ImportCategoriesAsync(plan.Rows, cancellationToken);
            case "brands":
                return await ImportBrandsAsync(plan.Rows, cancellationToken);
            case "product-types":
                return await ImportProductTypesAsync(plan.Rows, cancellationToken);
            case "units":
                return await ImportUnitsAsync(plan.Rows, cancellationToken);
            case "products":
                return await ImportProductsAsync(plan.Rows, cancellationToken);
            case "product-variants":
                return await ImportVariantsAsync(plan.Rows, cancellationToken);
            case "warehouses":
                return await ImportWarehousesAsync(plan.Rows, cancellationToken);
            default:
                throw new NotFoundException("inventory spreadsheet", plan.Section);
        }
    }

    private async Task<int> ImportCategoriesAsync(IReadOnlyList<ImportRow> rows, CancellationToken ct)
    {
        var service = Crud<ProductCategoryDto, CreateProductCategoryRequest, UpdateProductCategoryRequest>();
        var existing = await CategoriesAsync(new PageRequest(), ct);
        var ids = existing.ToDictionary(x => x.Code, x => x.Id, StringComparer.OrdinalIgnoreCase);
        var pending = rows.ToList();
        var imported = 0;

        while (pending.Count > 0)
        {
            var progress = false;
            foreach (var row in pending.ToArray())
            {
                var parentCode = row.Optional("ParentCode");
                if (parentCode is not null && !ids.ContainsKey(parentCode))
                    continue;

                var created = await service.CreateAsync(new CreateProductCategoryRequest(
                    row.Get("Code"),
                    row.Get("NameAr"),
                    row.Optional("NameEn"),
                    parentCode is null ? null : ids[parentCode]), ct);

                ids[created.Code] = created.Id;
                pending.Remove(row);
                imported++;
                progress = true;
            }

            if (!progress)
                throw new RequestValidationException(new Dictionary<string, string[]> { ["ParentCode"] = ["تعذر ترتيب التصنيفات حسب العلاقات الأب/الابن."] });
        }

        return imported;
    }

    private async Task<int> ImportBrandsAsync(IReadOnlyList<ImportRow> rows, CancellationToken ct)
    {
        var service = Crud<BrandDto, CreateBrandRequest, UpdateBrandRequest>();
        foreach (var row in rows)
            await service.CreateAsync(new CreateBrandRequest(row.Get("Code"), row.Get("Name")), ct);
        return rows.Count;
    }

    private async Task<int> ImportProductTypesAsync(IReadOnlyList<ImportRow> rows, CancellationToken ct)
    {
        var service = Crud<ProductTypeDto, CreateProductTypeRequest, UpdateProductTypeRequest>();
        foreach (var row in rows)
            await service.CreateAsync(new CreateProductTypeRequest(row.Get("Code"), row.Get("NameAr"), row.Optional("NameEn")), ct);
        return rows.Count;
    }

    private async Task<int> ImportUnitsAsync(IReadOnlyList<ImportRow> rows, CancellationToken ct)
    {
        var service = Crud<UnitDto, CreateUnitRequest, UpdateUnitRequest>();
        foreach (var row in rows)
            await service.CreateAsync(new CreateUnitRequest(row.Get("Code"), row.Get("NameAr"), row.Optional("NameEn")), ct);
        return rows.Count;
    }

    private async Task<int> ImportProductsAsync(IReadOnlyList<ImportRow> rows, CancellationToken ct)
    {
        var sender = Services.GetRequiredService<ISender>();
        var categories = (await CategoriesAsync(new PageRequest(), ct)).ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var brands = (await BrandsAsync(new PageRequest(), ct)).ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var productTypes = (await ProductTypesAsync(new PageRequest(), ct)).ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var units = (await UnitsAsync(new PageRequest(), ct)).ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var brandCode = row.Optional("BrandCode");
            var unitCode = row.Optional("InitialUnitCode");
            var sku = row.Optional("InitialSKU");
            var isStock = row.Get("IsStockItem").Length == 0 || row.Bool("IsStockItem", true);

            InitialProductVariantRequest? variant = null;
            if (sku is not null)
            {
                variant = new InitialProductVariantRequest(
                    sku,
                    row.Optional("InitialBarcode"),
                    row.Optional("InitialVariantName"),
                    row.Optional("InitialColor"),
                    row.Optional("InitialSize"),
                    unitCode is null ? null : units[unitCode].Id,
                    row.Decimal("InitialPurchasePrice"),
                    row.Decimal("InitialSellingPrice"));
            }

            InitialFrameDetailsRequest? frameDetails = null;
            if (HasAny(row, "FrameModel", "FrameMaterial", "FrameRimType", "FrameGender", "FrameShape", "FrameTempleLength", "FrameBridgeSize", "FrameLensWidth"))
            {
                frameDetails = new InitialFrameDetailsRequest(
                    row.Get("FrameModel"),
                    row.Optional("FrameMaterial"),
                    row.Optional("FrameRimType"),
                    row.Optional("FrameGender"),
                    row.Optional("FrameShape"),
                    row.NullableDecimal("FrameTempleLength"),
                    row.NullableDecimal("FrameBridgeSize"),
                    row.NullableDecimal("FrameLensWidth"));
            }

            InitialLensDetailsRequest? lensDetails = null;
            if (HasAny(row, "LensType", "LensMaterial", "LensCoating", "LensRefractiveIndex", "LensSphereMin", "LensSphereMax", "LensCylinderMin", "LensCylinderMax", "LensAddMin", "LensAddMax") || row.Bool("LensIsPrescription", false))
            {
                lensDetails = new InitialLensDetailsRequest(
                    row.Get("LensType"),
                    row.Optional("LensMaterial"),
                    row.Optional("LensCoating"),
                    row.NullableDecimal("LensRefractiveIndex"),
                    row.NullableDecimal("LensSphereMin"),
                    row.NullableDecimal("LensSphereMax"),
                    row.NullableDecimal("LensCylinderMin"),
                    row.NullableDecimal("LensCylinderMax"),
                    row.NullableDecimal("LensAddMin"),
                    row.NullableDecimal("LensAddMax"),
                    row.Bool("LensIsPrescription", false));
            }

            var request = new CreateStockProductRequest(
                new CreateProductRequest(
                    row.Get("ProductCode"),
                    row.Get("NameAr"),
                    row.Optional("NameEn"),
                    categories[row.Get("CategoryCode")].Id,
                    brandCode is null ? null : brands[brandCode].Id,
                    productTypes[row.Get("ProductTypeCode")].Id,
                    row.Optional("Description"),
                    isStock),
                variant,
                OpeningInventory: null,
                FrameDetails: frameDetails,
                LensDetails: lensDetails);

            await sender.Send(new CreateStockProductCommand(request), ct);
        }

        return rows.Count;
    }

    private async Task<int> ImportVariantsAsync(IReadOnlyList<ImportRow> rows, CancellationToken ct)
    {
        var service = Crud<ProductVariantDto, CreateProductVariantRequest, UpdateProductVariantRequest>();
        var products = (await ProductsAsync(new PageRequest(), ct)).ToDictionary(x => x.ProductCode, StringComparer.OrdinalIgnoreCase);
        var units = (await UnitsAsync(new PageRequest(), ct)).ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var unitCode = row.Optional("UnitCode");
            await service.CreateAsync(new CreateProductVariantRequest(
                products[row.Get("ProductCode")].Id,
                row.Get("SKU"),
                row.Optional("Barcode"),
                row.Optional("VariantName"),
                row.Optional("Color"),
                row.Optional("Size"),
                unitCode is null ? null : units[unitCode].Id,
                row.Decimal("PurchasePrice"),
                row.Decimal("SellingPrice")), ct);
        }

        return rows.Count;
    }

    private async Task<int> ImportWarehousesAsync(IReadOnlyList<ImportRow> rows, CancellationToken ct)
    {
        var service = Crud<WarehouseDto, CreateWarehouseRequest, UpdateWarehouseRequest>();
        foreach (var row in rows)
            await service.CreateAsync(new CreateWarehouseRequest(
                row.Get("Code"),
                row.Get("NameAr"),
                row.Optional("NameEn"),
                row.Optional("Description"),
                row.Bool("IsDefault", false)), ct);
        return rows.Count;
    }

    private static void ValidateCodeRows(
        IReadOnlyList<ImportRow> rows,
        string key,
        string header,
        IEnumerable<string> existingCodes,
        int maxLength)
    {
        var existing = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
        var file = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var code = row.Get(key);
            if (code.Length == 0) continue;
            if (code.Length > maxLength)
                row.Errors.Add($"{header}: الحد الأقصى {maxLength} حرفًا.");
            if (existing.Contains(code))
                row.Errors.Add($"{header}: مستخدم مسبقًا.");
            else if (!file.Add(code))
                row.Errors.Add($"{header}: مكرر داخل الملف.");
        }
    }

    private static bool HasAny(ImportRow row, params string[] keys) =>
        keys.Any(key => row.Optional(key) is not null);

    private static void ValidateDecimalPrecision(
        ImportRow row,
        string key,
        string header,
        int precision,
        int scale,
        bool allowNegative)
    {
        var value = row.Optional(key);
        if (value is null ||
            !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            return; // Type validation already reports malformed values.
        }

        if (!allowNegative && number < 0)
            return; // Generic/type validation already reports the non-negative requirement.

        if (number == decimal.MinValue)
        {
            row.Errors.Add($"{header}: القيمة خارج النطاق المسموح.");
            return;
        }

        var absolute = Math.Abs(number);
        var maxExclusive = Pow10(precision - scale);
        if (absolute >= maxExclusive || decimal.Round(number, scale) != number)
            row.Errors.Add($"{header}: يجب أن يطابق decimal({precision},{scale}).");
    }

    private static decimal Pow10(int exponent)
    {
        var value = 1m;
        for (var i = 0; i < exponent; i++) value *= 10m;
        return value;
    }

    private static void ValidateRequiredDecimal(ImportRow row, string key, string header, int scale)
    {
        var value = row.Get(key);
        if (value.Length == 0)
        {
            if (!row.Errors.Any(x => x.StartsWith(header + ":", StringComparison.Ordinal)))
                row.Errors.Add($"{header}: مطلوب.");
            return;
        }

        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
            return; // Type validation already adds the readable error.

        if (number < 0 || decimal.Round(number, scale) != number)
            row.Errors.Add($"{header}: يجب أن يكون رقمًا غير سالب وبحد أقصى {scale} منازل عشرية.");
    }

    private static void ValidateRequiredText(ImportRow row, string key, string header, int maxLength)
    {
        var value = row.Get(key);
        if (value.Length == 0)
        {
            if (!row.Errors.Any(x => x.StartsWith(header + ":", StringComparison.Ordinal)))
                row.Errors.Add($"{header}: مطلوب.");
            return;
        }
        if (value.Length > maxLength)
            row.Errors.Add($"{header}: الحد الأقصى {maxLength} حرفًا.");
    }

    private static void ValidateOptionalText(ImportRow row, string key, string header, int maxLength)
    {
        var value = row.Optional(key);
        if (value is not null && value.Length > maxLength)
            row.Errors.Add($"{header}: الحد الأقصى {maxLength} حرفًا.");
    }

    private static bool TryBool(string? value, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        return value.Trim().ToLowerInvariant() switch
        {
            "نعم" or "yes" or "true" or "1" => true,
            "لا" or "no" or "false" or "0" => false,
            _ => fallback
        };
    }

    private static bool Eq(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static SpreadsheetPreview InventoryFileValidationPreview(string message) =>
        new([new SpreadsheetRowResult("الملف", 1, new Dictionary<string, string>(), [message], [])]);

    private static bool IsSpreadsheetReadException(Exception ex) =>
        ex is InvalidDataException or IOException or ArgumentException or FormatException or InvalidOperationException or System.Xml.XmlException;
}
