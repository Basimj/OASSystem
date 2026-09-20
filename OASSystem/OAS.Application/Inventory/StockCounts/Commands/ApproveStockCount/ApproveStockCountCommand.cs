using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;

namespace OAS.Application.Inventory.StockCounts.Commands.ApproveStockCount;

public sealed record ApproveStockCountCommand(Guid StockCountId)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.StockCounts.Approve];
}
