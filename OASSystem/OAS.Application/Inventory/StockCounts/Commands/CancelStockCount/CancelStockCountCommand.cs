using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;

namespace OAS.Application.Inventory.StockCounts.Commands.CancelStockCount;

public sealed record CancelStockCountCommand(Guid StockCountId)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.StockCounts.Cancel];
}
