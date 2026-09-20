using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Transactions;
using DomainMovementType = OAS.Domain.Enums.Inventory.InventoryMovementType;

namespace OAS.Application.Inventory.Ledger.Queries.GetInventoryLedger;

public sealed record GetInventoryLedgerQuery(
    PageRequest Request,
    Guid? WarehouseId = null,
    Guid? ProductVariantId = null,
    Guid? TransactionId = null,
    DomainMovementType? MovementType = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null)
    : IQuery<PagedResult<InventoryLedgerDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.Ledger.View];
}
