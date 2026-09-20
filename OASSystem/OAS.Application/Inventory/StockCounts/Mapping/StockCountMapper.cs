using OAS.Contracts.Inventory.Stock;
using OAS.Domain.Entities.Inventory;
using ContractStatus = OAS.Contracts.Enums.Inventory.StockCountStatus;

namespace OAS.Application.Inventory.StockCounts.Mapping;

public sealed class StockCountMapper
{
    public StockCountDto ToRead(StockCount source)
    {
        return new StockCountDto(
            source.Id,
            source.CountNumber,
            source.WarehouseId,
            (ContractStatus)(int)source.Status,
            source.CountDate,
            source.StartedAtUtc,
            source.CompletedAtUtc,
            source.ApprovedAtUtc,
            source.ApprovedBy,
            source.PostedAtUtc,
            source.PostedBy,
            source.Notes);
    }
}
