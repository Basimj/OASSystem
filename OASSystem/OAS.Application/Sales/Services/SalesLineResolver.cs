using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.ValueObjects;

namespace OAS.Application.Sales.Services;

public sealed class SalesLineResolver(
    IReadRepository<ProductVariant, Guid> variants,
    IReadRepository<Product, Guid> products,
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
        if (lineType == SalesLineType.Service)
        {
            var serviceDescription = RequiredDescription(requestedDescription, "الخدمة");
            return new(null, null, null, serviceDescription, serviceDescription, null, 0m, false, null);
        }

        if (lineType == SalesLineType.Other && !productVariantId.HasValue)
        {
            var otherDescription = RequiredDescription(requestedDescription, "بند مبيعات");
            return new(null, null, null, otherDescription, otherDescription, null, 0m, false, null);
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

        if (lineType is SalesLineType.Frame or SalesLineType.Lens or SalesLineType.Accessory)
        {
            if (!product.IsStockItem)
                throw new ConflictException("sales_product_not_stock_item", "المنتج المحدد ليس منتج مخزون.");
            if (!warehouseId.HasValue || warehouseId.Value == Guid.Empty)
                throw new ConflictException("sales_warehouse_required", "يجب تحديد المخزن لهذا السطر.");
            var warehouse = await warehouses.GetByIdAsync(warehouseId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Warehouse), warehouseId.Value);
            if (!warehouse.IsActive)
                throw new ConflictException("sales_warehouse_inactive", "المخزن المحدد غير فعال.");
        }

        string? unitName = null;
        if (variant.UnitId.HasValue)
        {
            var unit = await units.GetByIdAsync(variant.UnitId.Value, cancellationToken);
            unitName = unit?.NameAr;
        }

        var prescriptionRequired = false;
        OpticalPrescriptionRangePolicy? policy = null;
        if (lineType == SalesLineType.Lens)
        {
            var lensSpec = new Specification<LensDetails>().Where(x => x.ProductId == product.Id);
            var lens = (await lensDetails.ListAsync(lensSpec, cancellationToken)).FirstOrDefault();
            if (lens is not null)
            {
                prescriptionRequired = lens.IsPrescriptionLens;
                if (lens.SphereMin.HasValue && lens.SphereMax.HasValue &&
                    lens.CylinderMin.HasValue && lens.CylinderMax.HasValue &&
                    lens.AddMin.HasValue && lens.AddMax.HasValue)
                {
                    policy = new OpticalPrescriptionRangePolicy(
                        new OpticalPowerRange(lens.SphereMin.Value, lens.SphereMax.Value),
                        new OpticalPowerRange(lens.CylinderMin.Value, lens.CylinderMax.Value),
                        new OpticalPowerRange(lens.AddMin.Value, lens.AddMax.Value));
                }
            }
        }

        var productName = string.IsNullOrWhiteSpace(variant.VariantName)
            ? product.NameAr
            : $"{product.NameAr} - {variant.VariantName}";
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
            policy);
    }

    private static string RequiredDescription(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
