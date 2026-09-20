using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Stock;

namespace OAS.Application.Inventory.StockCounts.Commands.CreateStockCount;

public sealed record CreateStockCountCommand(CreateStockCountRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.StockCounts.Create];
}
