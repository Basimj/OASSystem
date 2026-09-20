using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;

namespace OAS.Application.Inventory.StockCounts.Commands.PostStockCount;

public sealed record PostStockCountCommand(Guid StockCountId)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.StockCounts.Post];
}
