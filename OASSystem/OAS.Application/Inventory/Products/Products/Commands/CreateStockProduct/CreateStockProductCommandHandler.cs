using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Services;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using DomainFrameDetails = OAS.Domain.Entities.Inventory.FrameDetails;
using DomainLensDetails = OAS.Domain.Entities.Inventory.LensDetails;
using InventoryUnit = OAS.Domain.Entities.Inventory.Unit;

namespace OAS.Application.Inventory.Products.Products.Commands.CreateStockProduct;

public sealed class CreateStockProductCommandHandler(
    IRepository<Product, Guid> productRepository,
    IRepository<ProductVariant, Guid> variantRepository,
    IRepository<DomainFrameDetails, Guid> frameDetailsRepository,
    IRepository<DomainLensDetails, Guid> lensDetailsRepository,
    IReadRepository<ProductCategory, Guid> categoryRepository,
    IReadRepository<Brand, Guid> brandRepository,
    IReadRepository<ProductType, Guid> productTypeRepository,
    IReadRepository<InventoryUnit, Guid> unitRepository,
    IReadRepository<Warehouse, Guid> warehouseRepository,
    IInventoryTransactionRepository transactionRepository,
    IRepository<InventoryTransactionLine, Guid> lineRepository,
    IInventoryPostingService postingService,
    ISequenceNumberGenerator sequenceNumberGenerator,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<CreateStockProductCommand, CreateStockProductResult>
{
    public async Task<CreateStockProductResult> Handle(CreateStockProductCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var productRequest = request.Product;

        var category = await categoryRepository.GetByIdAsync(productRequest.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductCategory), productRequest.CategoryId);
        Ensure(category.IsActive, "product_category_inactive", "Selected product category is inactive.");

        if (productRequest.BrandId.HasValue)
        {
            var brand = await brandRepository.GetByIdAsync(productRequest.BrandId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Brand), productRequest.BrandId.Value);
            Ensure(brand.IsActive, "product_brand_inactive", "Selected brand is inactive.");
        }

        var productType = await productTypeRepository.GetByIdAsync(productRequest.ProductTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductType), productRequest.ProductTypeId);
        Ensure(productType.IsActive, "product_type_inactive", "Selected product type is inactive.");

        var isService = string.Equals(productType.SystemKey, ProductTypeSystemKeys.Service, StringComparison.OrdinalIgnoreCase);
        if (isService && productRequest.IsStockItem)
            throw Validation("product_type", "service_cannot_be_stock_item", "Service products cannot be stock items.");
        if (isService && request.OpeningInventory is not null)
            throw Validation("opening_inventory", "service_cannot_have_inventory", "Service products cannot have opening inventory.");

        if (productRequest.IsStockItem && request.Variant is null)
            throw Validation("variant", "stock_product_variant_required", "A stock product must have an initial variant.");

        var product = new Product(
            productRequest.ProductCode.Trim(),
            productRequest.NameAr.Trim(),
            productRequest.CategoryId,
            productRequest.ProductTypeId,
            productRequest.IsStockItem,
            NullIfBlank(productRequest.NameEn),
            productRequest.BrandId,
            NullIfBlank(productRequest.Description));
        await productRepository.AddAsync(product, cancellationToken);

        if (request.FrameDetails is not null)
        {
            if (!string.Equals(productType.SystemKey, ProductTypeSystemKeys.Frame, StringComparison.OrdinalIgnoreCase) && !string.Equals(productType.SystemKey, ProductTypeSystemKeys.Sunglasses, StringComparison.OrdinalIgnoreCase))
                throw Validation("frame_details", "frame_details_product_type_invalid", "Frame details are only valid for frame or sunglasses product types.");

            var frame = request.FrameDetails;
            await frameDetailsRepository.AddAsync(new DomainFrameDetails(
                product.Id,
                frame.Model.Trim(),
                NullIfBlank(frame.Material),
                NullIfBlank(frame.RimType),
                NullIfBlank(frame.Gender),
                NullIfBlank(frame.Shape),
                frame.TempleLength,
                frame.BridgeSize,
                frame.LensWidth), cancellationToken);
        }

        if (request.LensDetails is not null)
        {
            if (!string.Equals(productType.SystemKey, ProductTypeSystemKeys.Lens, StringComparison.OrdinalIgnoreCase))
                throw Validation("lens_details", "lens_details_product_type_invalid", "Lens details are only valid for lens products.");

            var lens = request.LensDetails;
            await lensDetailsRepository.AddAsync(new DomainLensDetails(
                product.Id,
                lens.LensType.Trim(),
                lens.IsPrescriptionLens,
                NullIfBlank(lens.Material),
                NullIfBlank(lens.Coating),
                lens.RefractiveIndex,
                lens.SphereMin,
                lens.SphereMax,
                lens.CylinderMin,
                lens.CylinderMax,
                lens.AddMin,
                lens.AddMax), cancellationToken);
        }

        ProductVariant? variant = null;
        if (request.Variant is not null)
        {
            var variantRequest = request.Variant;
            if (variantRequest.UnitId.HasValue)
            {
                var unit = await unitRepository.GetByIdAsync(variantRequest.UnitId.Value, cancellationToken)
                    ?? throw new NotFoundException("Unit", variantRequest.UnitId.Value);
                Ensure(unit.IsActive, "unit_inactive", "Selected unit is inactive.");
            }

            variant = new ProductVariant(
                product.Id,
                variantRequest.SKU.Trim(),
                variantRequest.PurchasePrice,
                variantRequest.SellingPrice,
                NullIfBlank(variantRequest.Barcode),
                NullIfBlank(variantRequest.VariantName),
                NullIfBlank(variantRequest.Color),
                NullIfBlank(variantRequest.Size),
                variantRequest.UnitId);
            await variantRepository.AddAsync(variant, cancellationToken);
        }

        Guid? transactionId = null;
        if (request.OpeningInventory is not null)
        {
            if (variant is null)
                throw Validation("variant", "opening_inventory_variant_required", "Opening inventory requires an active product variant.");

            var opening = request.OpeningInventory;
            Ensure(opening.Quantity > 0m, "opening_quantity_must_be_positive", "Opening quantity must be greater than zero.");
            Ensure(opening.UnitCost >= 0m, "opening_unit_cost_invalid", "Opening unit cost cannot be negative.");
            Ensure(product.IsActive && product.IsStockItem, "opening_inventory_requires_stock_product", "Opening inventory requires an active stock product.");
            Ensure(variant.IsActive, "product_variant_inactive", "Opening inventory requires an active product variant.");

            var warehouse = await warehouseRepository.GetByIdAsync(opening.WarehouseId, cancellationToken)
                ?? throw new NotFoundException(nameof(Warehouse), opening.WarehouseId);
            Ensure(warehouse.IsActive, "warehouse_inactive", "Selected warehouse is inactive.");

            var now = timeProvider.GetUtcNow();
            var seq = await sequenceNumberGenerator.NextAsync("InventoryTransaction", cancellationToken);
            var transaction = new InventoryTransaction(
                $"TXN-{now.Year:0000}-{seq:000000}",
                InventoryTransactionType.Opening,
                now,
                destinationWarehouseId: warehouse.Id,
                referenceType: "ProductOpening",
                referenceId: product.Id,
                reason: "Opening inventory from product creation");
            await transactionRepository.AddAsync(transaction, cancellationToken);

            var line = new InventoryTransactionLine(
                transaction.Id,
                variant.Id,
                opening.Quantity,
                opening.UnitCost,
                "Initial stock created with product");
            await lineRepository.AddAsync(line, cancellationToken);

            var userId = currentUser.UserId ?? "system";
            await postingService.PostMovementAsync(
                warehouse.Id,
                variant.Id,
                InventoryMovementType.In,
                opening.Quantity,
                opening.UnitCost,
                transaction.Id,
                line.Id,
                transaction.TransactionDate,
                userId,
                cancellationToken);

            transaction.Post(now, userId);
            transactionRepository.Update(transaction);
            transactionId = transaction.Id;
        }

        return new CreateStockProductResult(
            product.Id,
            product.ProductCode,
            variant?.Id,
            variant?.SKU,
            request.OpeningInventory is not null,
            request.OpeningInventory?.WarehouseId,
            request.OpeningInventory?.Quantity,
            request.OpeningInventory?.UnitCost,
            transactionId);
    }

    private static void Ensure(bool condition, string code, string message)
    {
        if (!condition)
            throw Validation("request", code, message);
    }

    private static RequestValidationException Validation(string field, string code, string message) =>
        new(new Dictionary<string, string[]> { [field] = [$"{code}: {message}"] });

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
