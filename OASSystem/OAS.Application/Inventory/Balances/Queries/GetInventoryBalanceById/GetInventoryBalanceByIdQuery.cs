using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Stock;

namespace OAS.Application.Inventory.Balances.Queries.GetInventoryBalanceById;

public sealed record GetInventoryBalanceByIdQuery(Guid Id)
    : IQuery<InventoryBalanceDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.Balances.View];
}
