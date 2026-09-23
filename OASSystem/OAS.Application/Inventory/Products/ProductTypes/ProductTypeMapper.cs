using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Products.ProductTypes;

public sealed class ProductTypeMapper
    : ICrudMapper<ProductType, Guid, ProductTypeDto, CreateProductTypeRequest, UpdateProductTypeRequest>
{
    public ProductType Create(CreateProductTypeRequest source)
    {
        return new ProductType(source.Code.Trim().ToUpperInvariant(), source.NameAr.Trim(), source.NameEn?.Trim());
    }

    public void Update(UpdateProductTypeRequest source, ProductType destination)
    {
        destination.UpdateDetails(source.Code.Trim().ToUpperInvariant(), source.NameAr.Trim(), source.NameEn?.Trim(), source.IsActive);
    }

    public ProductTypeDto ToRead(ProductType source)
    {
        return new ProductTypeDto(
            source.Id,
            source.Code,
            source.NameAr,
            source.NameEn,
            source.SystemKey,
            source.IsActive);
    }
}
