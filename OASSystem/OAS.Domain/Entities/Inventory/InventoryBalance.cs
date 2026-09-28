using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Entities.Inventory;

public class InventoryBalance : AuditableEntity<Guid>
{
    public Guid WarehouseId { get; private set; }
    public Guid ProductVariantId { get; private set; }

    public decimal OnHandQuantity { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public decimal OnOrderQuantity { get; private set; }

    public decimal AverageUnitCost { get; private set; }
    public decimal InventoryValue { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public DateTimeOffset? LastMovementAtUtc { get; private set; }

    private InventoryBalance()
    {
    }

    public InventoryBalance(
        Guid warehouseId,
        Guid productVariantId,
        decimal onHandQuantity = 0,
        decimal reservedQuantity = 0,
        decimal onOrderQuantity = 0,
        decimal averageUnitCost = 0,
        decimal inventoryValue = 0)
    {
        Id = Guid.NewGuid();

        WarehouseId = warehouseId;
        ProductVariantId = productVariantId;

        OnHandQuantity = onHandQuantity;
        ReservedQuantity = reservedQuantity;
        OnOrderQuantity = onOrderQuantity;

        AverageUnitCost = averageUnitCost;
        InventoryValue = inventoryValue;
    }

    public decimal AvailableQuantity =>
        OnHandQuantity - ReservedQuantity;

    public void Reserve(decimal quantity)
    {
        EnsurePositive(quantity, "Reservation quantity");

        if (AvailableQuantity < quantity)
            throw new DomainException("Insufficient available inventory quantity for reservation.");

        ReservedQuantity += quantity;
    }

    public void ReleaseReservation(decimal quantity)
    {
        EnsurePositive(quantity, "Reservation release quantity");

        if (ReservedQuantity < quantity)
            throw new DomainException("Reservation release quantity exceeds the reserved quantity.");

        ReservedQuantity -= quantity;
    }

    public void ConsumeReservation(decimal quantity, DateTimeOffset movementAtUtc)
    {
        EnsurePositive(quantity, "Reservation consumption quantity");

        if (ReservedQuantity < quantity)
            throw new DomainException("Reservation consumption quantity exceeds the reserved quantity.");

        if (OnHandQuantity < quantity)
            throw new DomainException("Insufficient on-hand inventory quantity to consume the reservation.");

        ReservedQuantity -= quantity;
        OnHandQuantity -= quantity;
        RecalculateInventoryValue();
        LastMovementAtUtc = movementAtUtc;
    }

    public void ApplyInbound(decimal quantity, decimal unitCost, DateTimeOffset movementAtUtc)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Inbound quantity must be greater than zero.");

        var oldOnHand = OnHandQuantity;
        var oldInventoryValue = InventoryValue;
        var incomingValue = quantity * unitCost;

        OnHandQuantity += quantity;

        if (OnHandQuantity > 0)
        {
            if (oldOnHand <= 0)
            {
                AverageUnitCost = unitCost;
                InventoryValue = Math.Round(OnHandQuantity * unitCost, 2);
            }
            else
            {
                var newTotalValue = Math.Max(0, oldInventoryValue) + incomingValue;
                AverageUnitCost = Math.Round(newTotalValue / OnHandQuantity, 2);
                InventoryValue = Math.Round(OnHandQuantity * AverageUnitCost, 2);
            }
        }
        else
        {
            InventoryValue = 0;
        }

        LastMovementAtUtc = movementAtUtc;
    }

    public void ApplyOutbound(decimal quantity, DateTimeOffset movementAtUtc)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Outbound quantity must be greater than zero.");

        if (AvailableQuantity < quantity)
            throw new DomainException("Insufficient available inventory quantity. Reserved stock cannot be issued by a normal outbound movement.");

        OnHandQuantity -= quantity;
        RecalculateInventoryValue();
        LastMovementAtUtc = movementAtUtc;
    }

    public void ApplyAdjustment(decimal differenceQuantity, decimal unitCostSnapshot, DateTimeOffset movementAtUtc)
    {
        if (differenceQuantity > 0)
        {
            ApplyInbound(differenceQuantity, unitCostSnapshot > 0 ? unitCostSnapshot : AverageUnitCost, movementAtUtc);
        }
        else if (differenceQuantity < 0)
        {
            ApplyOutbound(Math.Abs(differenceQuantity), movementAtUtc);
        }
    }

    private void RecalculateInventoryValue()
    {
        InventoryValue = Math.Round(Math.Max(0, OnHandQuantity * AverageUnitCost), 2);
    }

    private static void EnsurePositive(decimal quantity, string name)
    {
        if (quantity <= 0)
            throw new DomainException($"{name} must be greater than zero.");
    }
}
