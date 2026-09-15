using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class InventoryLedger : Entity<Guid>
{
    public long SequenceNumber { get; private set; }

    public Guid TransactionId { get; private set; }
    public Guid TransactionLineId { get; private set; }

    public Guid WarehouseId { get; private set; }
    public Guid ProductVariantId { get; private set; }

    public string MovementType { get; private set; } = null!;

    public decimal QuantityIn { get; private set; }
    public decimal QuantityOut { get; private set; }

    public decimal BalanceAfter { get; private set; }

    public decimal UnitCost { get; private set; }
    public decimal AverageCostAfter { get; private set; }
    public decimal InventoryValueAfter { get; private set; }

    public DateTimeOffset MovementDate { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }

    private InventoryLedger()
    {
    }

    public InventoryLedger(
        long sequenceNumber,
        Guid transactionId,
        Guid transactionLineId,
        Guid warehouseId,
        Guid productVariantId,
        string movementType,
        decimal quantityIn,
        decimal quantityOut,
        decimal balanceAfter,
        decimal unitCost,
        decimal averageCostAfter,
        decimal inventoryValueAfter,
        DateTimeOffset movementDate,
        DateTimeOffset createdAtUtc,
        string? createdBy = null)
    {
        Id = Guid.NewGuid();

        SequenceNumber = sequenceNumber;

        TransactionId = transactionId;
        TransactionLineId = transactionLineId;

        WarehouseId = warehouseId;
        ProductVariantId = productVariantId;

        MovementType = movementType;

        QuantityIn = quantityIn;
        QuantityOut = quantityOut;

        BalanceAfter = balanceAfter;

        UnitCost = unitCost;
        AverageCostAfter = averageCostAfter;
        InventoryValueAfter = inventoryValueAfter;

        MovementDate = movementDate;
        CreatedAtUtc = createdAtUtc;
        CreatedBy = createdBy;
    }
}