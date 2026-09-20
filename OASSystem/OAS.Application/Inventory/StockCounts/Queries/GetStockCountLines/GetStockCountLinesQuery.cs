using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Stock;

namespace OAS.Application.Inventory.StockCounts.Queries.GetStockCountLines;

public sealed record GetStockCountLinesQuery(Guid StockCountId)
    : IQuery<IReadOnlyList<StockCountLineDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.StockCounts.View];
}
