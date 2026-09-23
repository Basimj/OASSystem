using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Products.Brands;

public sealed class BrandMapper
    : ICrudMapper<Brand, Guid, BrandDto, CreateBrandRequest, UpdateBrandRequest>
{
    public Brand Create(CreateBrandRequest source)
    {
        return new Brand(source.Code, source.Name);
    }

    public void Update(UpdateBrandRequest source, Brand destination)
    {
        destination.UpdateDetails(source.Code.Trim(), source.Name.Trim(), source.IsActive);
    }

    public BrandDto ToRead(Brand source)
    {
        return new BrandDto(
            source.Id,
            source.Code,
            source.Name,
            source.IsActive);
    }
}
