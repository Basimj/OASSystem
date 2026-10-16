using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Inventory.Warehouses;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Warehouses;

public sealed class WarehouseMapper
    : ICrudMapper<Warehouse, Guid, WarehouseDto, CreateWarehouseRequest, UpdateWarehouseRequest>
{
    public Warehouse Create(CreateWarehouseRequest source)
    {
        return new Warehouse(
            source.Code,
            source.Name,
            source.Description,
            source.IsDefault,
            source.AllowsSalesAllocation);
    }

    public void Update(UpdateWarehouseRequest source, Warehouse destination)
    {
        destination.UpdateDetails(
            source.Code.Trim(),
            source.Name,
            source.Description,
            source.IsDefault,
            source.IsActive,
            source.AllowsSalesAllocation);
    }

    public WarehouseDto ToRead(Warehouse source)
    {
        return new WarehouseDto(
            source.Id,
            source.Code,
            source.Name,
            source.Description,
            source.IsDefault,
            source.IsActive,
            source.AllowsSalesAllocation);
    }
}
