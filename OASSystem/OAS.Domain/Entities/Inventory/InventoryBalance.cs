using OAS.Domain.Common.Entities;

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
}