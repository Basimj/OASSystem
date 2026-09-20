using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Transactions;

namespace OAS.Application.Inventory.Ledger.Queries.GetInventoryLedgerById;

public sealed record GetInventoryLedgerByIdQuery(Guid Id)
    : IQuery<InventoryLedgerDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.Ledger.View];
}
