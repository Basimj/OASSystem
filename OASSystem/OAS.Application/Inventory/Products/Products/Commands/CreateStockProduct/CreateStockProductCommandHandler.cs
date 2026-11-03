using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;
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
    IReadRepository<InventoryUnit, Guid> unitRepository)
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

        var productType = await productTypeRepository.GetByIdAsync(category.ProductTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductType), category.ProductTypeId);
        Ensure(productType.IsActive, "product_type_inactive", "Selected product type is inactive.");

        var isService = string.Equals(productType.SystemKey, ProductTypeSystemKeys.Service, StringComparison.OrdinalIgnoreCase);
        if (isService && productRequest.IsStockItem)
            throw Validation("product_type", "service_cannot_be_stock_item", "Service products cannot be stock items.");
        // Enforce the policy even for direct handler callers that bypass FluentValidation.
        if (request.OpeningInventory is not null)
            throw Validation("opening_inventory", "opening_inventory_disabled", "لا يمكن إضافة رصيد عند إنشاء المنتج؛ تتم زيادة المخزون عبر ترحيل استلام المشتريات فقط.");

        if (productRequest.IsStockItem && request.Variant is null)
            throw Validation("variant", "stock_product_variant_required", "A stock product must have an initial variant.");

        var product = new Product(
            productRequest.ProductCode.Trim(),
            productRequest.Name.Trim(),
            productRequest.CategoryId,
            productRequest.IsStockItem,
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

        if (request.LensDetails is not null)
        {
            if (!string.Equals(productType.SystemKey, ProductTypeSystemKeys.Lens, StringComparison.OrdinalIgnoreCase))
                throw Validation("lens_details", "lens_details_product_type_invalid", "Lens details are only valid for lens products.");
            if (variant is null)
                throw Validation("variant", "lens_details_variant_required", "Lens details require a product variant because optical measurements identify the SKU.");

            var lens = request.LensDetails;
            await lensDetailsRepository.AddAsync(new DomainLensDetails(
                variant.Id,
                lens.LensType.Trim(),
                lens.IsPrescriptionLens,
                NullIfBlank(lens.Material),
                NullIfBlank(lens.Coating),
                lens.RefractiveIndex,
                lens.SPH,
                lens.CYL,
                lens.ADD,
                lens.BaseCurve,
                lens.Diameter), cancellationToken);
        }

        // Product creation is catalog-only: inbound stock must be posted by an approved purchase receipt.
        // Keep the response shape compatible with existing clients, but never create inventory here.
        return new CreateStockProductResult(
            product.Id, product.ProductCode, variant?.Id, variant?.SKU,
            false, null, null, null, null);
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
