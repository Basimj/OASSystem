using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Transactions;

namespace OAS.Application.Inventory.Transactions.Queries.GetInventoryTransactionLines;

public sealed record GetInventoryTransactionLinesQuery(Guid TransactionId)
    : IQuery<IReadOnlyList<InventoryTransactionLineDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.Transactions.View];
}
