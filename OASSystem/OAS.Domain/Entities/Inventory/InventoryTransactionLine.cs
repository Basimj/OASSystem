using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class InventoryTransactionLine : Entity<Guid>
{
    public Guid TransactionId { get; private set; }
    public Guid ProductVariantId { get; private set; }

    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal TotalCost { get; private set; }

    public string? Notes { get; private set; }

    private InventoryTransactionLine()
    {
    }

    public InventoryTransactionLine(
        Guid transactionId,
        Guid productVariantId,
        decimal quantity,
        decimal unitCost,
        string? notes = null)
    {
        Id = Guid.NewGuid();

        TransactionId = transactionId;
        ProductVariantId = productVariantId;

        Quantity = quantity;
        UnitCost = unitCost;
        TotalCost = quantity * unitCost;

        Notes = notes;
    }
    // Persist the actual unit cost applied by the inventory posting service.
    public void ApplyPostedUnitCost(decimal unitCost)
    {
        if (unitCost < 0m)
            throw new System.ArgumentOutOfRangeException(nameof(unitCost), "Unit cost cannot be negative.");

        UnitCost = unitCost;
        TotalCost = Math.Round(Quantity * UnitCost, 2);
    }

    // Match the inventory valuation rounding for opening and opening-reversal lines.
    public void RoundOpeningValue() => TotalCost = Math.Round(Quantity * UnitCost, 2);
}
