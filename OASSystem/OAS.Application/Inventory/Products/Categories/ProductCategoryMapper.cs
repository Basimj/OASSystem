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
            source.NameAr,
            source.NameEn,
            source.ParentCategoryId);
    }

    public void Update(UpdateProductCategoryRequest source, ProductCategory destination)
    {
        destination.UpdateDetails(
            source.NameAr,
            source.NameEn,
            source.ParentCategoryId,
            source.IsActive);
    }

    public ProductCategoryDto ToRead(ProductCategory source)
    {
        return new ProductCategoryDto(
            source.Id,
            source.Code,
            source.NameAr,
            source.NameEn,
            source.ParentCategoryId,
            source.IsActive);
    }
}
