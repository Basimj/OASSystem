using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;
using ContractProductType = OAS.Contracts.Enums.Inventory.ProductType;
using DomainProductType = OAS.Domain.Enums.Inventory.ProductType;

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
            (DomainProductType)(int)source.ProductType,
            source.IsStockItem,
            source.NameEn,
            source.BrandId,
            source.Description);
    }

    public void Update(UpdateProductRequest source, Product destination)
    {
        destination.UpdateDetails(
            source.NameAr,
            source.NameEn,
            source.CategoryId,
            source.BrandId,
            (DomainProductType)(int)source.ProductType,
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
            (ContractProductType)(int)source.ProductType,
            source.Description,
            source.IsStockItem,
            source.IsActive);
    }
}
