using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;

namespace OAS.Application.Inventory.StockCounts.Commands.StartStockCount;

public sealed record StartStockCountCommand(Guid StockCountId)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.StockCounts.Start];
}
