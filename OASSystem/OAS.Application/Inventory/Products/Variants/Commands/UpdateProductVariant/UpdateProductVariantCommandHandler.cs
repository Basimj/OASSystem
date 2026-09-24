using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;
using InventoryUnit = OAS.Domain.Entities.Inventory.Unit;

namespace OAS.Application.Inventory.Products.Variants.Commands.UpdateProductVariant;

public sealed class UpdateProductVariantCommandHandler(
    IRepository<ProductVariant, Guid> variantRepository,
    IReadRepository<Product, Guid> productRepository,
    IReadRepository<InventoryUnit, Guid> unitRepository,
    ProductVariantMapper mapper)
    : IRequestHandler<UpdateProductVariantCommand, ProductVariantDto>
{
    public async Task<ProductVariantDto> Handle(UpdateProductVariantCommand command, CancellationToken cancellationToken)
    {
        var variant = await variantRepository.GetForUpdateAsync(command.VariantId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductVariant), command.VariantId);
        var product = await productRepository.GetByIdAsync(variant.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), variant.ProductId);

        if (command.Request.UnitId.HasValue)
        {
            var unit = await unitRepository.GetByIdAsync(command.Request.UnitId.Value, cancellationToken)
                ?? throw new NotFoundException("Unit", command.Request.UnitId.Value);
            if (!unit.IsActive)
                throw Validation("unit", "unit_inactive", "Selected unit is inactive.");
        }

        if (product.IsStockItem && variant.IsActive && !command.Request.IsActive)
        {
            var otherActiveSpec = new Specification<ProductVariant>()
                .Where(x => x.ProductId == product.Id && x.Id != variant.Id && x.IsActive);
            if (await variantRepository.CountAsync(otherActiveSpec, cancellationToken) == 0)
            {
                throw Validation(
                    "is_active",
                    "stock_product_requires_active_variant",
                    "A stock product must keep at least one active product variant.");
            }
        }

        variant.UpdateDetails(
            command.Request.SKU.Trim(),
            NullIfBlank(command.Request.Barcode),
            NullIfBlank(command.Request.VariantName),
            NullIfBlank(command.Request.Color),
            NullIfBlank(command.Request.Size),
            command.Request.UnitId,
            command.Request.PurchasePrice,
            command.Request.SellingPrice,
            command.Request.IsActive);

        variantRepository.Update(variant);
        return mapper.ToRead(variant);
    }

    private static RequestValidationException Validation(string field, string code, string message) =>
        new(new Dictionary<string, string[]> { [field] = [$"{code}: {message}"] });

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
