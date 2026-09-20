using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Stock;

namespace OAS.Application.Inventory.StockCounts.Commands.RecordStockCount;

public sealed record RecordStockCountCommand(
    Guid StockCountId,
    Guid LineId,
    RecordStockCountRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.StockCounts.Record];
}
