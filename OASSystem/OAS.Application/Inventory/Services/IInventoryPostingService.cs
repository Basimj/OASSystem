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

    Task<decimal> GetAvailableOutboundUnitCostAsync(
        Guid warehouseId,
        Guid productVariantId,
        decimal quantity,
        CancellationToken cancellationToken = default);

    Task<decimal> GetOutboundUnitCostAsync(
        Guid warehouseId,
        Guid productVariantId,
        decimal quantity,
        CancellationToken cancellationToken = default);

    Task<(InventoryBalance Balance, InventoryLedger Ledger)> PostReservedOutboundAsync(
        Guid warehouseId,
        Guid productVariantId,
        decimal quantity,
        Guid transactionId,
        Guid transactionLineId,
        DateTimeOffset movementDate,
        string createdBy,
        CancellationToken cancellationToken = default);
}
