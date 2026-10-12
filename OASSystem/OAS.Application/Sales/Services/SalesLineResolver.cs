using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

public sealed class SalesLineResolver(
    IReadRepository<ProductVariant, Guid> variants,
    IReadRepository<Product, Guid> products,
    IReadRepository<ProductCategory, Guid> categories,
    IReadRepository<ProductType, Guid> productTypes,
    IReadRepository<Unit, Guid> units,
    IReadRepository<Warehouse, Guid> warehouses,
    IReadRepository<LensDetails, Guid> lensDetails) : ISalesLineResolver
{
    public async Task<SalesLineResolution> ResolveAsync(
        SalesLineType lineType,
        Guid? productVariantId,
        Guid? warehouseId,
        string? requestedDescription,
        CancellationToken cancellationToken = default)
    {
        if (lineType == SalesLineType.Service && !productVariantId.HasValue)
        {
            var serviceDescription = RequiredDescription(requestedDescription, "الخدمة");
            return new(null, null, null, serviceDescription, serviceDescription, null, 0m, false, false);
        }

        if (lineType == SalesLineType.Other && !productVariantId.HasValue)
        {
            var otherDescription = RequiredDescription(requestedDescription, "بند مبيعات");
            return new(null, null, null, otherDescription, otherDescription, null, 0m, false, false);
        }

        if (!productVariantId.HasValue || productVariantId.Value == Guid.Empty)
            throw new ConflictException("sales_product_required", "يجب تحديد المنتج لهذا السطر.");

        var variant = await variants.GetByIdAsync(productVariantId.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductVariant), productVariantId.Value);
        if (!variant.IsActive)
            throw new ConflictException("sales_product_variant_inactive", "متغير المنتج المحدد غير فعال.");

        var product = await products.GetByIdAsync(variant.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), variant.ProductId);
        if (!product.IsActive)
            throw new ConflictException("sales_product_inactive", "المنتج المحدد غير فعال.");

        var category = await categories.GetByIdAsync(product.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductCategory), product.CategoryId);
        if (!category.IsActive)
            throw new ConflictException("sales_product_category_inactive", "تصنيف المنتج المحدد غير فعال.");

        var productType = await productTypes.GetByIdAsync(category.ProductTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductType), category.ProductTypeId);
        if (!productType.IsActive)
            throw new ConflictException("sales_product_type_inactive", "نوع المنتج المحدد غير فعال.");

        var expectedLineType = SalesProductTypeMapping.ToLineType(productType.SystemKey);
        if (lineType != expectedLineType)
            throw new ConflictException("sales_product_type_mismatch", $"نوع سطر البيع لا يتطابق مع نوع المنتج ({productType.Name}). أعد اختيار المنتج من النوع الصحيح.");

        if (product.IsStockItem)
        {
            if (!warehouseId.HasValue || warehouseId.Value == Guid.Empty)
                throw new ConflictException("sales_warehouse_required", "يجب تحديد المخزن لهذا المنتج.");
            var warehouse = await warehouses.GetByIdAsync(warehouseId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Warehouse), warehouseId.Value);
            if (!warehouse.IsActive)
                throw new ConflictException("sales_warehouse_inactive", "المخزن المحدد غير فعال.");
        }
        else if (warehouseId.HasValue && warehouseId.Value != Guid.Empty)
        {
            throw new ConflictException("sales_warehouse_not_allowed", "لا يتم تحديد مخزن لمنتج غير مخزني.");
        }

        string? unitName = null;
        if (variant.UnitId.HasValue)
        {
            var unit = await units.GetByIdAsync(variant.UnitId.Value, cancellationToken);
            unitName = unit?.Name;
        }

        var prescriptionRequired = false;
        if (lineType == SalesLineType.Lens)
        {
            var lens = (await lensDetails.ListAsync(
                new Specification<LensDetails>().Where(x => x.ProductVariantId == variant.Id && x.IsActive),
                cancellationToken)).SingleOrDefault();
            if (lens is null)
                throw new ConflictException("sales_lens_details_required", "متغير العدسة المحدد لا يحتوي على بيانات قياس LensDetails. أكمل بيانات العدسة في المخزون أولاً.");
            prescriptionRequired = lens.IsPrescriptionLens;
        }

        var productName = string.IsNullOrWhiteSpace(variant.VariantName)
            ? product.Name
            : $"{product.Name} - {variant.VariantName}";
        var description = string.IsNullOrWhiteSpace(requestedDescription) ? productName : requestedDescription.Trim();

        return new(
            variant.Id,
            warehouseId,
            variant.SKU,
            productName,
            description,
            unitName,
            variant.SellingPrice,
            prescriptionRequired,
            product.IsStockItem);
    }

    private static string RequiredDescription(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
