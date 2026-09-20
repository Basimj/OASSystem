using OAS.Contracts.Inventory.Stock;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.StockCounts.Mapping;

public sealed class StockCountLineMapper
{
    public StockCountLineDto ToRead(StockCountLine source)
    {
        return new StockCountLineDto(
            source.Id,
            source.StockCountId,
            source.ProductVariantId,
            source.SystemQuantity,
            source.CountedQuantity,
            source.DifferenceQuantity,
            source.AverageCostSnapshot,
            source.VarianceValue,
            source.CountedAtUtc,
            source.CountedBy,
            source.Notes);
    }
}
