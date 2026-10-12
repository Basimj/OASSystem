using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Products.Categories;

public sealed class ProductCategoryMapper
    : ICrudMapper<ProductCategory, Guid, ProductCategoryDto, CreateProductCategoryRequest, UpdateProductCategoryRequest>
{
    public ProductCategory Create(CreateProductCategoryRequest source)
    {
        return new ProductCategory(
            source.Code,
            source.Name,
            source.ProductTypeId);
    }

    public void Update(UpdateProductCategoryRequest source, ProductCategory destination)
    {
        destination.UpdateDetails(
            source.Code.Trim(),
            source.Name,
            source.ProductTypeId,
            source.IsActive);
    }

    public ProductCategoryDto ToRead(ProductCategory source)
    {
        return new ProductCategoryDto(
            source.Id,
            source.Code,
            source.Name,
            source.ProductTypeId,
            source.IsActive);
    }
}
