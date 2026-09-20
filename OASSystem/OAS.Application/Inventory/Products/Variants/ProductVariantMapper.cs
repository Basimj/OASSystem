using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Products.Variants;

public sealed class ProductVariantMapper
    : ICrudMapper<ProductVariant, Guid, ProductVariantDto, CreateProductVariantRequest, UpdateProductVariantRequest>
{
    public ProductVariant Create(CreateProductVariantRequest source)
    {
        return new ProductVariant(
            source.ProductId,
            source.SKU,
            source.PurchasePrice,
            source.SellingPrice,
            source.Barcode,
            source.VariantName,
            source.Color,
            source.Size,
            source.UnitId);
    }

    public void Update(UpdateProductVariantRequest source, ProductVariant destination)
    {
        destination.UpdateDetails(
            source.Barcode,
            source.VariantName,
            source.Color,
            source.Size,
            source.UnitId,
            source.PurchasePrice,
            source.SellingPrice,
            source.IsActive);
    }

    public ProductVariantDto ToRead(ProductVariant source)
    {
        return new ProductVariantDto(
            source.Id,
            source.ProductId,
            source.SKU,
            source.Barcode,
            source.VariantName,
            source.Color,
            source.Size,
            source.UnitId,
            source.PurchasePrice,
            source.SellingPrice,
            source.IsActive);
    }
}
