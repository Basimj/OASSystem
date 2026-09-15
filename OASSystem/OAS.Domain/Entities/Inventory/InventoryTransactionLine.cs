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
        decimal unitCost = 0,
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
}