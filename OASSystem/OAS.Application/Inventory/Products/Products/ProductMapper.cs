using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Products.Products;

public sealed class ProductMapper
    : ICrudMapper<Product, Guid, ProductDto, CreateProductRequest, UpdateProductRequest>
{
    public Product Create(CreateProductRequest source)
    {
        return new Product(
            source.ProductCode,
            source.NameAr,
            source.CategoryId,
            source.ProductTypeId,
            source.IsStockItem,
            source.NameEn,
            source.BrandId,
            source.Description);
    }

    public void Update(UpdateProductRequest source, Product destination)
    {
        destination.UpdateDetails(
            source.ProductCode,
            source.NameAr,
            source.NameEn,
            source.CategoryId,
            source.BrandId,
            source.ProductTypeId,
            source.Description,
            source.IsStockItem,
            source.IsActive);
    }

    public ProductDto ToRead(Product source)
    {
        return new ProductDto(
            source.Id,
            source.ProductCode,
            source.NameAr,
            source.NameEn,
            source.CategoryId,
            source.BrandId,
            source.ProductTypeId,
            source.Description,
            source.IsStockItem,
            source.IsActive);
    }
}
