using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Stock;

namespace OAS.Application.Inventory.Balances.Queries.GetInventoryBalanceByWarehouseAndVariant;

public sealed record GetInventoryBalanceByWarehouseAndVariantQuery(
    Guid WarehouseId,
    Guid ProductVariantId)
    : IQuery<InventoryBalanceDto?>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.Balances.View];
}
