using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Products.Units;

public sealed class UnitMapper
    : ICrudMapper<Unit, Guid, UnitDto, CreateUnitRequest, UpdateUnitRequest>
{
    public Unit Create(CreateUnitRequest source)
    {
        return new Unit(source.Code, source.Name);
    }

    public void Update(UpdateUnitRequest source, Unit destination)
    {
        destination.UpdateDetails(source.Code.Trim(), source.Name, source.IsActive);
    }

    public UnitDto ToRead(Unit source)
    {
        return new UnitDto(
            source.Id,
            source.Code,
            source.Name,
            source.IsActive);
    }
}
