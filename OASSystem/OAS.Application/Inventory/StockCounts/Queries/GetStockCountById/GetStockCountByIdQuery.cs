using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Stock;

namespace OAS.Application.Inventory.StockCounts.Queries.GetStockCountById;

public sealed record GetStockCountByIdQuery(Guid Id)
    : IQuery<StockCountDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.StockCounts.View];
}
