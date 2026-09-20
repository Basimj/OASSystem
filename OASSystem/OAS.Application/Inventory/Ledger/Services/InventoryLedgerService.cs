using MediatR;
using OAS.Application.Inventory.Ledger.Queries.GetInventoryLedger;
using OAS.Application.Inventory.Ledger.Queries.GetInventoryLedgerById;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Transactions;
using DomainMovementType = OAS.Domain.Enums.Inventory.InventoryMovementType;

namespace OAS.Application.Inventory.Ledger.Services;

public sealed class InventoryLedgerService(ISender sender) : IInventoryLedgerService
{
    public Task<InventoryLedgerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        sender.Send(new GetInventoryLedgerByIdQuery(id), cancellationToken);

    public Task<PagedResult<InventoryLedgerDto>> GetPageAsync(
        PageRequest request,
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        Guid? transactionId = null,
        DomainMovementType? movementType = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default) =>
        sender.Send(new GetInventoryLedgerQuery(request, warehouseId, productVariantId, transactionId, movementType, fromDate, toDate), cancellationToken);
}
