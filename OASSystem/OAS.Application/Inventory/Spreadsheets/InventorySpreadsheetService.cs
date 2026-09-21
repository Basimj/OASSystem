using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.CRUD.Abstractions;
using OAS.Application.Inventory.Authorization;
using OAS.Application.Inventory.Balances.Services;
using OAS.Application.Inventory.Ledger.Services;
using OAS.Application.Inventory.StockCounts.Services;
using OAS.Application.Inventory.Transactions.Services;
using OAS.Application.Spreadsheets;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Enums.Inventory;
using OAS.Contracts.Inventory.Products;
using OAS.Contracts.Inventory.Stock;
using OAS.Contracts.Inventory.Transactions;
using OAS.Contracts.Inventory.Warehouses;
using DomainMovementType = OAS.Domain.Enums.Inventory.InventoryMovementType;
using DomainStockCountStatus = OAS.Domain.Enums.Inventory.StockCountStatus;
using DomainTransactionStatus = OAS.Domain.Enums.Inventory.InventoryTransactionStatus;
using DomainTransactionType = OAS.Domain.Enums.Inventory.InventoryTransactionType;

namespace OAS.Application.Inventory.Spreadsheets;

/// <summary>
/// Read-only Excel exports for the Inventory V1 screens. The export intentionally
/// uses the same application services/specifications as the UI and never writes data.
/// </summary>
public sealed class InventorySpreadsheetService(
    ISpreadsheetWorkbook workbook,
    IServiceProvider services,
    IPermissionChecker permissions)
{
    public static readonly string[] ExportSections =
    [
        "product-categories", "brands", "units", "products", "product-variants",
        "warehouses", "balances", "transactions", "ledger", "stock-counts"
    ];

    public async Task<byte[]> ExportAsync(
        string section,
        PageRequest request,
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        Guid? transactionId = null,
        int? transactionType = null,
        int? transactionStatus = null,
        int? movementType = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int? stockCountStatus = null,
        CancellationToken cancellationToken = default)
    {
        section = section.Trim().ToLowerInvariant();
        if (!ExportSections.Contains(section, StringComparer.Ordinal))
            throw new NotFoundException("inventory spreadsheet", section);

        await AuthorizeAsync(section, cancellationToken);

        var tables = section switch
        {
            "product-categories" => await ExportCategoriesAsync(request, cancellationToken),
            "brands" => await ExportBrandsAsync(request, cancellationToken),
            "units" => await ExportUnitsAsync(request, cancellationToken),
            "products" => await ExportProductsAsync(request, cancellationToken),
            "product-variants" => await ExportVariantsAsync(request, cancellationToken),
            "warehouses" => await ExportWarehousesAsync(request, cancellationToken),
            "balances" => await ExportBalancesAsync(request, warehouseId, productVariantId, cancellationToken),
            "transactions" => await ExportTransactionsAsync(request, warehouseId, transactionType, transactionStatus, fromDate, toDate, cancellationToken),
            "ledger" => await ExportLedgerAsync(request, warehouseId, productVariantId, transactionId, movementType, fromDate, toDate, cancellationToken),
            "stock-counts" => await ExportStockCountsAsync(request, warehouseId, stockCountStatus, cancellationToken),
            _ => throw new NotFoundException("inventory spreadsheet", section)
        };

        return workbook.Write(tables);
    }

    private async Task AuthorizeAsync(string section, CancellationToken cancellationToken)
    {
        var permission = section switch
        {
            "product-categories" => InventoryPermissions.ProductCategories.View,
            "brands" => InventoryPermissions.Brands.View,
            "units" => InventoryPermissions.Units.View,
            "products" => InventoryPermissions.Products.View,
            "product-variants" => InventoryPermissions.ProductVariants.View,
            "warehouses" => InventoryPermissions.Warehouses.View,
            "balances" => InventoryPermissions.Balances.View,
            "transactions" => InventoryPermissions.Transactions.View,
            "ledger" => InventoryPermissions.Ledger.View,
            "stock-counts" => InventoryPermissions.StockCounts.View,
            _ => throw new NotFoundException("inventory spreadsheet", section)
        };

        if (!await permissions.HasPermissionAsync(permission, cancellationToken))
            throw new ForbiddenException();
    }

    private async Task<IReadOnlyList<T>> ReadAllAsync<T>(
        Func<PageRequest, Task<PagedResult<T>>> query,
        PageRequest request)
    {
        var result = new List<T>();
        var baseRequest = request.Normalize();
        for (var pageNumber = 1; ; pageNumber++)
        {
            var page = await query(baseRequest with
            {
                PageNumber = pageNumber,
                PageSize = PageRequest.MaximumPageSize
            });
            result.AddRange(page.Items);
            if (page.Items.Count == 0 || result.Count >= page.TotalCount)
                break;
        }
        return result;
    }

    private ICrudApplicationService<Guid, TDto, TCreate, TUpdate> Crud<TDto, TCreate, TUpdate>() =>
        services.GetRequiredService<ICrudApplicationService<Guid, TDto, TCreate, TUpdate>>();

    private Task<IReadOnlyList<ProductCategoryDto>> CategoriesAsync(PageRequest request, CancellationToken ct) =>
        ReadAllAsync(p => Crud<ProductCategoryDto, CreateProductCategoryRequest, UpdateProductCategoryRequest>().GetPageAsync(p, ct), request);

    private Task<IReadOnlyList<BrandDto>> BrandsAsync(PageRequest request, CancellationToken ct) =>
        ReadAllAsync(p => Crud<BrandDto, CreateBrandRequest, UpdateBrandRequest>().GetPageAsync(p, ct), request);

    private Task<IReadOnlyList<UnitDto>> UnitsAsync(PageRequest request, CancellationToken ct) =>
        ReadAllAsync(p => Crud<UnitDto, CreateUnitRequest, UpdateUnitRequest>().GetPageAsync(p, ct), request);

    private Task<IReadOnlyList<ProductDto>> ProductsAsync(PageRequest request, CancellationToken ct) =>
        ReadAllAsync(p => Crud<ProductDto, CreateProductRequest, UpdateProductRequest>().GetPageAsync(p, ct), request);

    private Task<IReadOnlyList<ProductVariantDto>> VariantsAsync(PageRequest request, CancellationToken ct) =>
        ReadAllAsync(p => Crud<ProductVariantDto, CreateProductVariantRequest, UpdateProductVariantRequest>().GetPageAsync(p, ct), request);

    private Task<IReadOnlyList<WarehouseDto>> WarehousesAsync(PageRequest request, CancellationToken ct) =>
        ReadAllAsync(p => Crud<WarehouseDto, CreateWarehouseRequest, UpdateWarehouseRequest>().GetPageAsync(p, ct), request);

    private async Task<IReadOnlyList<SpreadsheetTable>> ExportCategoriesAsync(PageRequest request, CancellationToken ct)
    {
        var categories = await CategoriesAsync(request, ct);
        var all = await CategoriesAsync(new PageRequest(), ct);
        var lookup = all.ToDictionary(x => x.Id, x => $"{x.Code} - {x.NameAr}");
        return [Table("التصنيفات",
            [Text("الكود"), Text("الاسم العربي"), Text("الاسم الإنجليزي"), Text("التصنيف الأب"), Text("الحالة")],
            categories.Select(x => Row(
                ("الكود", x.Code),
                ("الاسم العربي", x.NameAr),
                ("الاسم الإنجليزي", x.NameEn),
                ("التصنيف الأب", x.ParentCategoryId is Guid id ? lookup.GetValueOrDefault(id) : null),
                ("الحالة", Active(x.IsActive)))))];
    }

    private async Task<IReadOnlyList<SpreadsheetTable>> ExportBrandsAsync(PageRequest request, CancellationToken ct)
    {
        var items = await BrandsAsync(request, ct);
        return [Table("الماركات",
            [Text("الكود"), Text("الاسم"), Text("الحالة")],
            items.Select(x => Row(("الكود", x.Code), ("الاسم", x.Name), ("الحالة", Active(x.IsActive)))))];
    }

    private async Task<IReadOnlyList<SpreadsheetTable>> ExportUnitsAsync(PageRequest request, CancellationToken ct)
    {
        var items = await UnitsAsync(request, ct);
        return [Table("الوحدات",
            [Text("الكود"), Text("الاسم العربي"), Text("الاسم الإنجليزي"), Text("الحالة")],
            items.Select(x => Row(("الكود", x.Code), ("الاسم العربي", x.NameAr), ("الاسم الإنجليزي", x.NameEn), ("الحالة", Active(x.IsActive)))))];
    }

    private async Task<IReadOnlyList<SpreadsheetTable>> ExportProductsAsync(PageRequest request, CancellationToken ct)
    {
        var items = await ProductsAsync(request, ct);
        var categories = (await CategoriesAsync(new PageRequest(), ct)).ToDictionary(x => x.Id, x => $"{x.Code} - {x.NameAr}");
        var brands = (await BrandsAsync(new PageRequest(), ct)).ToDictionary(x => x.Id, x => $"{x.Code} - {x.Name}");
        return [Table("المنتجات",
            [Text("كود المنتج"), Text("الاسم العربي"), Text("الاسم الإنجليزي"), Text("التصنيف"), Text("الماركة"), Text("نوع المنتج"), Text("الوصف"), Text("صنف مخزني"), Text("الحالة")],
            items.Select(x => Row(
                ("كود المنتج", x.ProductCode),
                ("الاسم العربي", x.NameAr),
                ("الاسم الإنجليزي", x.NameEn),
                ("التصنيف", categories.GetValueOrDefault(x.CategoryId)),
                ("الماركة", x.BrandId is Guid brandId ? brands.GetValueOrDefault(brandId) : null),
                ("نوع المنتج", ProductTypeText(x.ProductType)),
                ("الوصف", x.Description),
                ("صنف مخزني", YesNo(x.IsStockItem)),
                ("الحالة", Active(x.IsActive)))))];
    }

    private async Task<IReadOnlyList<SpreadsheetTable>> ExportVariantsAsync(PageRequest request, CancellationToken ct)
    {
        var items = await VariantsAsync(request, ct);
        var products = (await ProductsAsync(new PageRequest(), ct)).ToDictionary(x => x.Id, x => $"{x.ProductCode} - {x.NameAr}");
        var units = (await UnitsAsync(new PageRequest(), ct)).ToDictionary(x => x.Id, x => $"{x.Code} - {x.NameAr}");
        return [Table("متغيرات المنتجات",
            [Text("رمز SKU"), Text("الباركود"), Text("المنتج"), Text("اسم المتغير"), Text("اللون"), Text("المقاس"), Text("الوحدة"), Number("سعر الشراء"), Number("سعر البيع"), Text("الحالة")],
            items.Select(x => Row(
                ("رمز SKU", x.SKU),
                ("الباركود", x.Barcode),
                ("المنتج", products.GetValueOrDefault(x.ProductId)),
                ("اسم المتغير", x.VariantName),
                ("اللون", x.Color),
                ("المقاس", x.Size),
                ("الوحدة", x.UnitId is Guid unitId ? units.GetValueOrDefault(unitId) : null),
                ("سعر الشراء", F(x.PurchasePrice)),
                ("سعر البيع", F(x.SellingPrice)),
                ("الحالة", Active(x.IsActive)))))];
    }

    private async Task<IReadOnlyList<SpreadsheetTable>> ExportWarehousesAsync(PageRequest request, CancellationToken ct)
    {
        var items = await WarehousesAsync(request, ct);
        return [Table("المخازن",
            [Text("الكود"), Text("الاسم العربي"), Text("الاسم الإنجليزي"), Text("الوصف"), Text("المخزن الافتراضي"), Text("الحالة")],
            items.Select(x => Row(
                ("الكود", x.Code), ("الاسم العربي", x.NameAr), ("الاسم الإنجليزي", x.NameEn), ("الوصف", x.Description),
                ("المخزن الافتراضي", YesNo(x.IsDefault)), ("الحالة", Active(x.IsActive)))))];
    }

    private async Task<IReadOnlyList<SpreadsheetTable>> ExportBalancesAsync(
        PageRequest request, Guid? warehouseId, Guid? productVariantId, CancellationToken ct)
    {
        var service = services.GetRequiredService<IInventoryBalanceService>();
        var sourceRequest = request with { Search = null };
        var items = await ReadAllAsync(p => service.GetPageAsync(p, warehouseId, productVariantId, ct), sourceRequest);
        var products = await ProductsAsync(new PageRequest(), ct);
        var variants = await VariantsAsync(new PageRequest(), ct);
        var warehouses = await WarehousesAsync(new PageRequest(), ct);
        var productMap = products.ToDictionary(x => x.Id);
        var variantMap = variants.ToDictionary(x => x.Id);
        var warehouseMap = warehouses.ToDictionary(x => x.Id);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            items = items.Where(x =>
            {
                variantMap.TryGetValue(x.ProductVariantId, out var variant);
                ProductDto? product = variant is null ? null : productMap.GetValueOrDefault(variant.ProductId);
                var warehouse = warehouseMap.GetValueOrDefault(x.WarehouseId);
                return Match(term, variant?.SKU, variant?.Barcode, variant?.VariantName, product?.ProductCode, product?.NameAr, product?.NameEn, warehouse?.NameAr);
            }).ToArray();
        }

        return [Table("أرصدة المخزون",
            [Text("المنتج"), Text("متغير المنتج"), Text("رمز SKU"), Text("الباركود"), Text("المخزن"), Number("المتوفر فعليًا"), Number("المحجوز"), Number("المتاح"), Number("قيد الطلب"), Number("متوسط التكلفة"), Number("قيمة المخزون"), Text("آخر حركة")],
            items.Select(x =>
            {
                variantMap.TryGetValue(x.ProductVariantId, out var variant);
                var product = variant is null ? null : productMap.GetValueOrDefault(variant.ProductId);
                return Row(
                    ("المنتج", product is null ? null : $"{product.ProductCode} - {product.NameAr}"),
                    ("متغير المنتج", variant?.VariantName),
                    ("رمز SKU", variant?.SKU),
                    ("الباركود", variant?.Barcode),
                    ("المخزن", warehouseMap.GetValueOrDefault(x.WarehouseId)?.NameAr),
                    ("المتوفر فعليًا", F(x.OnHandQuantity)),
                    ("المحجوز", F(x.ReservedQuantity)),
                    ("المتاح", F(x.AvailableQuantity)),
                    ("قيد الطلب", F(x.OnOrderQuantity)),
                    ("متوسط التكلفة", F(x.AverageUnitCost)),
                    ("قيمة المخزون", F(x.InventoryValue)),
                    ("آخر حركة", DateTimeText(x.LastMovementAtUtc)));
            }))];
    }

    private async Task<IReadOnlyList<SpreadsheetTable>> ExportTransactionsAsync(
        PageRequest request, Guid? warehouseId, int? transactionType, int? transactionStatus,
        DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct)
    {
        var service = services.GetRequiredService<IInventoryTransactionService>();
        var type = ToEnum<DomainTransactionType>(transactionType);
        var status = ToEnum<DomainTransactionStatus>(transactionStatus);
        var sourceRequest = request with { Search = null };
        var items = await ReadAllAsync(p => service.GetPageAsync(p, type, status, warehouseId, fromDate, toDate, ct), sourceRequest);
        var warehouses = (await WarehousesAsync(new PageRequest(), ct)).ToDictionary(x => x.Id);
        var products = (await ProductsAsync(new PageRequest(), ct)).ToDictionary(x => x.Id);
        var variants = (await VariantsAsync(new PageRequest(), ct)).ToDictionary(x => x.Id);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            items = items.Where(x => Match(term,
                x.TransactionNumber, x.ReferenceType, x.Reason, x.Notes,
                x.SourceWarehouseId is Guid source ? warehouses.GetValueOrDefault(source)?.NameAr : null,
                x.DestinationWarehouseId is Guid destination ? warehouses.GetValueOrDefault(destination)?.NameAr : null)).ToArray();
        }

        var lineRows = new List<IReadOnlyDictionary<string, string>>();
        foreach (var transaction in items)
        {
            var lines = await service.GetLinesAsync(transaction.Id, ct);
            foreach (var line in lines)
            {
                variants.TryGetValue(line.ProductVariantId, out var variant);
                var product = variant is null ? null : products.GetValueOrDefault(variant.ProductId);
                lineRows.Add(Row(
                    ("رقم العملية", transaction.TransactionNumber),
                    ("المنتج", product is null ? null : $"{product.ProductCode} - {product.NameAr}"),
                    ("متغير المنتج", variant?.VariantName),
                    ("رمز SKU", variant?.SKU),
                    ("الباركود", variant?.Barcode),
                    ("الكمية", F(line.Quantity)),
                    ("تكلفة الوحدة", F(line.UnitCost)),
                    ("الإجمالي", F(line.TotalCost)),
                    ("ملاحظات", line.Notes)));
            }
        }

        return
        [
            Table("عمليات المخزون",
                [Text("رقم العملية"), Text("نوع العملية"), Text("التاريخ"), Text("من مخزن"), Text("إلى مخزن"), Text("الحالة"), Text("نوع المرجع"), Text("معرف المرجع"), Text("السبب"), Text("ملاحظات"), Text("تاريخ الترحيل"), Text("رحّل بواسطة")],
                items.Select(x => Row(
                    ("رقم العملية", x.TransactionNumber),
                    ("نوع العملية", TransactionTypeText(x.TransactionType)),
                    ("التاريخ", DateTimeText(x.TransactionDate)),
                    ("من مخزن", x.SourceWarehouseId is Guid source ? warehouses.GetValueOrDefault(source)?.NameAr : null),
                    ("إلى مخزن", x.DestinationWarehouseId is Guid destination ? warehouses.GetValueOrDefault(destination)?.NameAr : null),
                    ("الحالة", TransactionStatusText(x.Status)),
                    ("نوع المرجع", x.ReferenceType),
                    ("معرف المرجع", x.ReferenceId?.ToString("D")),
                    ("السبب", x.Reason),
                    ("ملاحظات", x.Notes),
                    ("تاريخ الترحيل", DateTimeText(x.PostedAtUtc)),
                    ("رحّل بواسطة", x.PostedBy)))),
            Table("بنود العمليات",
                [Text("رقم العملية"), Text("المنتج"), Text("متغير المنتج"), Text("رمز SKU"), Text("الباركود"), Number("الكمية"), Number("تكلفة الوحدة"), Number("الإجمالي"), Text("ملاحظات")],
                lineRows)
        ];
    }

    private async Task<IReadOnlyList<SpreadsheetTable>> ExportLedgerAsync(
        PageRequest request, Guid? warehouseId, Guid? productVariantId, Guid? transactionId,
        int? movementType, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct)
    {
        var service = services.GetRequiredService<IInventoryLedgerService>();
        var movement = ToEnum<DomainMovementType>(movementType);
        var sourceRequest = request with { Search = null };
        var items = await ReadAllAsync(p => service.GetPageAsync(p, warehouseId, productVariantId, transactionId, movement, fromDate, toDate, ct), sourceRequest);
        var transactionService = services.GetRequiredService<IInventoryTransactionService>();
        var transactions = await ReadAllAsync(p => transactionService.GetPageAsync(p, cancellationToken: ct), new PageRequest());
        var transactionMap = transactions.ToDictionary(x => x.Id);
        var warehouses = (await WarehousesAsync(new PageRequest(), ct)).ToDictionary(x => x.Id);
        var products = (await ProductsAsync(new PageRequest(), ct)).ToDictionary(x => x.Id);
        var variants = (await VariantsAsync(new PageRequest(), ct)).ToDictionary(x => x.Id);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            items = items.Where(x =>
            {
                transactionMap.TryGetValue(x.TransactionId, out var transaction);
                variants.TryGetValue(x.ProductVariantId, out var variant);
                var product = variant is null ? null : products.GetValueOrDefault(variant.ProductId);
                var warehouse = warehouses.GetValueOrDefault(x.WarehouseId);
                return Match(term,
                    transaction?.TransactionNumber, variant?.VariantName, variant?.SKU, variant?.Barcode,
                    product?.ProductCode, product?.NameAr, warehouse?.NameAr,
                    transaction is null ? null : TransactionTypeText(transaction.TransactionType),
                    transaction?.ReferenceType, transaction?.Reason, transaction?.Notes);
            }).ToArray();
        }

        return [Table("حركات المخزون",
            [Text("التسلسل"), Text("التاريخ"), Text("رقم العملية"), Text("نوع الحركة"), Text("المنتج"), Text("متغير المنتج"), Text("رمز SKU"), Text("الباركود"), Text("المخزن"), Number("الكمية الداخلة"), Number("الكمية الخارجة"), Number("الرصيد بعد الحركة"), Number("التكلفة"), Number("متوسط التكلفة"), Number("قيمة المخزون"), Text("مصدر الحركة"), Text("أنشئ بواسطة")],
            items.Select(x =>
            {
                transactionMap.TryGetValue(x.TransactionId, out var transaction);
                variants.TryGetValue(x.ProductVariantId, out var variant);
                var product = variant is null ? null : products.GetValueOrDefault(variant.ProductId);
                var source = transaction is null
                    ? null
                    : string.Join(" - ", new[] { transaction.ReferenceType, transaction.ReferenceId?.ToString("D") }.Where(v => !string.IsNullOrWhiteSpace(v)));
                return Row(
                    ("التسلسل", x.SequenceNumber.ToString(CultureInfo.InvariantCulture)),
                    ("التاريخ", DateTimeText(x.MovementDate)),
                    ("رقم العملية", transaction?.TransactionNumber),
                    ("نوع الحركة", MovementTypeText(x.MovementType)),
                    ("المنتج", product is null ? null : $"{product.ProductCode} - {product.NameAr}"),
                    ("متغير المنتج", variant?.VariantName),
                    ("رمز SKU", variant?.SKU),
                    ("الباركود", variant?.Barcode),
                    ("المخزن", warehouses.GetValueOrDefault(x.WarehouseId)?.NameAr),
                    ("الكمية الداخلة", F(x.QuantityIn)),
                    ("الكمية الخارجة", F(x.QuantityOut)),
                    ("الرصيد بعد الحركة", F(x.BalanceAfter)),
                    ("التكلفة", F(x.UnitCost)),
                    ("متوسط التكلفة", F(x.AverageCostAfter)),
                    ("قيمة المخزون", F(x.InventoryValueAfter)),
                    ("مصدر الحركة", source),
                    ("أنشئ بواسطة", x.CreatedBy));
            }))];
    }

    private async Task<IReadOnlyList<SpreadsheetTable>> ExportStockCountsAsync(
        PageRequest request, Guid? warehouseId, int? stockCountStatus, CancellationToken ct)
    {
        var service = services.GetRequiredService<IStockCountService>();
        var status = ToEnum<DomainStockCountStatus>(stockCountStatus);
        var sourceRequest = request with { Search = null };
        var items = await ReadAllAsync(p => service.GetPageAsync(p, warehouseId, status, cancellationToken: ct), sourceRequest);
        var warehouses = (await WarehousesAsync(new PageRequest(), ct)).ToDictionary(x => x.Id);
        var products = (await ProductsAsync(new PageRequest(), ct)).ToDictionary(x => x.Id);
        var variants = (await VariantsAsync(new PageRequest(), ct)).ToDictionary(x => x.Id);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            items = items.Where(x => Match(term, x.CountNumber, x.Notes, warehouses.GetValueOrDefault(x.WarehouseId)?.NameAr)).ToArray();
        }

        var lineRows = new List<IReadOnlyDictionary<string, string>>();
        foreach (var count in items)
        {
            var lines = await service.GetLinesAsync(count.Id, ct);
            foreach (var line in lines)
            {
                variants.TryGetValue(line.ProductVariantId, out var variant);
                var product = variant is null ? null : products.GetValueOrDefault(variant.ProductId);
                lineRows.Add(Row(
                    ("رقم الجرد", count.CountNumber),
                    ("المنتج", product is null ? null : $"{product.ProductCode} - {product.NameAr}"),
                    ("متغير المنتج", variant?.VariantName),
                    ("رمز SKU", variant?.SKU),
                    ("الباركود", variant?.Barcode),
                    ("كمية النظام", F(line.SystemQuantity)),
                    ("الكمية الفعلية", F(line.CountedQuantity)),
                    ("الفرق", F(line.DifferenceQuantity)),
                    ("متوسط التكلفة", F(line.AverageCostSnapshot)),
                    ("قيمة الفرق", F(line.VarianceValue)),
                    ("وقت العد", DateTimeText(line.CountedAtUtc)),
                    ("عُد بواسطة", line.CountedBy),
                    ("ملاحظات", line.Notes)));
            }
        }

        return
        [
            Table("الجرد المخزني",
                [Text("رقم الجرد"), Text("المخزن"), Text("الحالة"), Text("تاريخ الجرد"), Text("بدء العد"), Text("إكمال العد"), Text("الاعتماد"), Text("اعتمد بواسطة"), Text("الترحيل"), Text("رحّل بواسطة"), Text("ملاحظات")],
                items.Select(x => Row(
                    ("رقم الجرد", x.CountNumber),
                    ("المخزن", warehouses.GetValueOrDefault(x.WarehouseId)?.NameAr),
                    ("الحالة", StockCountStatusText(x.Status)),
                    ("تاريخ الجرد", x.CountDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    ("بدء العد", DateTimeText(x.StartedAtUtc)),
                    ("إكمال العد", DateTimeText(x.CompletedAtUtc)),
                    ("الاعتماد", DateTimeText(x.ApprovedAtUtc)),
                    ("اعتمد بواسطة", x.ApprovedBy),
                    ("الترحيل", DateTimeText(x.PostedAtUtc)),
                    ("رحّل بواسطة", x.PostedBy),
                    ("ملاحظات", x.Notes)))),
            Table("بنود الجرد",
                [Text("رقم الجرد"), Text("المنتج"), Text("متغير المنتج"), Text("رمز SKU"), Text("الباركود"), Number("كمية النظام"), Number("الكمية الفعلية"), Number("الفرق"), Number("متوسط التكلفة"), Number("قيمة الفرق"), Text("وقت العد"), Text("عُد بواسطة"), Text("ملاحظات")],
                lineRows)
        ];
    }

    private static SpreadsheetTable Table(string sheetName, IReadOnlyList<SpreadsheetColumn> columns, IEnumerable<IReadOnlyDictionary<string, string>> rows) =>
        new(new SpreadsheetSheet(sheetName, columns), rows.ToArray());

    private static SpreadsheetColumn Text(string key) => new(key);
    private static SpreadsheetColumn Number(string key) => new(key, DataType: "decimal");

    private static IReadOnlyDictionary<string, string> Row(params (string Key, string? Value)[] values) =>
        values.ToDictionary(x => x.Key, x => x.Value ?? string.Empty, StringComparer.Ordinal);

    private static TEnum? ToEnum<TEnum>(int? value) where TEnum : struct, Enum
    {
        if (!value.HasValue) return null;
        if (!Enum.IsDefined(typeof(TEnum), value.Value))
            throw new RequestValidationException(new Dictionary<string, string[]> { ["filter"] = ["قيمة الفلتر غير صالحة."] });
        return (TEnum)Enum.ToObject(typeof(TEnum), value.Value);
    }

    private static bool Match(string search, params string?[] values) =>
        values.Any(value => value?.Contains(search, StringComparison.CurrentCultureIgnoreCase) == true);

    private static string F(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    private static string? DateTimeText(DateTimeOffset? value) => value?.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
    private static string DateTimeText(DateTimeOffset value) => value.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
    private static string Active(bool value) => value ? "نشط" : "غير نشط";
    private static string YesNo(bool value) => value ? "نعم" : "لا";

    private static string ProductTypeText(ProductType type) => type switch
    {
        ProductType.Frame => "إطار",
        ProductType.Lens => "عدسة",
        ProductType.Sunglasses => "نظارة شمسية",
        ProductType.Accessory => "إكسسوار",
        ProductType.Other => "أخرى",
        ProductType.Service => "خدمة",
        _ => type.ToString()
    };

    private static string TransactionTypeText(InventoryTransactionType type) => type switch
    {
        InventoryTransactionType.Opening => "رصيد افتتاحي",
        InventoryTransactionType.Receipt => "إدخال مخزون",
        InventoryTransactionType.Issue => "إخراج مخزون",
        InventoryTransactionType.Transfer => "تحويل بين المخازن",
        InventoryTransactionType.AdjustmentIncrease => "تعديل زيادة",
        InventoryTransactionType.AdjustmentDecrease => "تعديل نقص",
        InventoryTransactionType.SalesReturn => "مرتجع بيع",
        InventoryTransactionType.PurchaseReturn => "مرتجع شراء",
        InventoryTransactionType.ProductionIssue => "صرف إنتاج",
        InventoryTransactionType.Scrap => "تالف / Scrap",
        _ => type.ToString()
    };

    private static string TransactionStatusText(InventoryTransactionStatus status) => status switch
    {
        InventoryTransactionStatus.Draft => "مسودة",
        InventoryTransactionStatus.Posted => "مرحّلة",
        _ => status.ToString()
    };

    private static string MovementTypeText(InventoryMovementType type) => type switch
    {
        InventoryMovementType.In => "وارد",
        InventoryMovementType.Out => "صادر",
        _ => type.ToString()
    };

    private static string StockCountStatusText(StockCountStatus status) => status switch
    {
        StockCountStatus.Draft => "مسودة",
        StockCountStatus.Counting => "قيد العد",
        StockCountStatus.Review => "مراجعة",
        StockCountStatus.Approved => "معتمد",
        StockCountStatus.Posted => "مرحّل",
        StockCountStatus.Cancelled => "ملغي",
        _ => status.ToString()
    };
}
