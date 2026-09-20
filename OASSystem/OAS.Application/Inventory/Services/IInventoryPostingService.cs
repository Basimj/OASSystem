using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;

namespace OAS.Application.Inventory.Services;

public interface IInventoryPostingService
{
    Task<(InventoryBalance Balance, InventoryLedger Ledger)> PostMovementAsync(
        Guid warehouseId,
        Guid productVariantId,
        InventoryMovementType movementType,
        decimal quantity,
        decimal unitCost,
        Guid transactionId,
        Guid transactionLineId,
        DateTimeOffset movementDate,
        string createdBy,
        CancellationToken cancellationToken = default);
}
