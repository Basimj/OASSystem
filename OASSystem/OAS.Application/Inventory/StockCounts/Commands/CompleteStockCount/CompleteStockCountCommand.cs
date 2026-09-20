using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;

namespace OAS.Application.Inventory.StockCounts.Commands.CompleteStockCount;

public sealed record CompleteStockCountCommand(Guid StockCountId)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.StockCounts.Complete];
}
