using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Transactions;
using DomainMovementType = OAS.Domain.Enums.Inventory.InventoryMovementType;

namespace OAS.Application.Inventory.Ledger.Services;

public interface IInventoryLedgerService
{
    Task<InventoryLedgerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<InventoryLedgerDto>> GetPageAsync(
        PageRequest request,
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        Guid? transactionId = null,
        DomainMovementType? movementType = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);
}
