using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Transactions;

namespace OAS.Application.Inventory.Transactions.Queries.GetInventoryTransactionById;

public sealed record GetInventoryTransactionByIdQuery(Guid Id)
    : IQuery<InventoryTransactionDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.Transactions.View];
}
