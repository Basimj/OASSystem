using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Products.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    IRepository<Product, Guid> productRepository,
    IReadRepository<ProductVariant, Guid> variantRepository,
    IReadRepository<InventoryBalance, Guid> balanceRepository,
    IReadRepository<InventoryLedger, Guid> ledgerRepository,
    IReadRepository<ProductCategory, Guid> categoryRepository,
    IReadRepository<Brand, Guid> brandRepository,
    IReadRepository<ProductType, Guid> productTypeRepository,
    ProductMapper mapper)
    : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var product = await productRepository.GetForUpdateAsync(command.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), command.ProductId);

        var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductCategory), request.CategoryId);
        Ensure(category.IsActive, "product_category_inactive", "Selected product category is inactive.");

        if (request.BrandId.HasValue)
        {
            var brand = await brandRepository.GetByIdAsync(request.BrandId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Brand), request.BrandId.Value);
            Ensure(brand.IsActive, "product_brand_inactive", "Selected brand is inactive.");
        }

        var productType = await productTypeRepository.GetByIdAsync(request.ProductTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductType), request.ProductTypeId);
        Ensure(productType.IsActive, "product_type_inactive", "Selected product type is inactive.");

        var isService = string.Equals(productType.SystemKey, ProductTypeSystemKeys.Service, StringComparison.OrdinalIgnoreCase);
        if (isService && request.IsStockItem)
            throw Validation("product_type", "service_cannot_be_stock_item", "Service products cannot be stock items.");

        var productVariants = await variantRepository.ListAsync(
            new Specification<ProductVariant>().Where(x => x.ProductId == command.ProductId),
            cancellationToken);

        if (request.IsStockItem && productVariants.All(x => !x.IsActive))
        {
            throw Validation(
                "variant",
                "stock_product_variant_required",
                "A stock product must have at least one active product variant.");
        }

        if (!request.IsStockItem && productVariants.Count > 0)
        {
            var variantIds = productVariants.Select(x => x.Id).ToArray();
            var hasBalance = await balanceRepository.CountAsync(
                new Specification<InventoryBalance>().Where(x => variantIds.Contains(x.ProductVariantId)),
                cancellationToken) > 0;
            var hasLedger = await ledgerRepository.CountAsync(
                new Specification<InventoryLedger>().Where(x => variantIds.Contains(x.ProductVariantId)),
                cancellationToken) > 0;

            if (hasBalance || hasLedger)
            {
                throw Validation(
                    "is_stock_item",
                    isService ? "service_product_has_inventory_history" : "stock_product_has_inventory_history",
                    "A product with inventory balance or movement history must remain a stock product.");
            }
        }

        product.UpdateDetails(
            request.ProductCode.Trim(),
            request.NameAr.Trim(),
            NullIfBlank(request.NameEn),
            request.CategoryId,
            request.BrandId,
            request.ProductTypeId,
            NullIfBlank(request.Description),
            request.IsStockItem,
            request.IsActive);

        productRepository.Update(product);
        return mapper.ToRead(product);
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
