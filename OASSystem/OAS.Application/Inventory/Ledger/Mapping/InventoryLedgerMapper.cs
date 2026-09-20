using OAS.Contracts.Inventory.Transactions;
using OAS.Domain.Entities.Inventory;
using ContractMovementType = OAS.Contracts.Enums.Inventory.InventoryMovementType;

namespace OAS.Application.Inventory.Ledger.Mapping;

public sealed class InventoryLedgerMapper
{
    public InventoryLedgerDto ToRead(InventoryLedger source)
    {
        return new InventoryLedgerDto(
            source.Id,
            source.SequenceNumber,
            source.TransactionId,
            source.TransactionLineId,
            source.WarehouseId,
            source.ProductVariantId,
            (ContractMovementType)(int)source.MovementType,
            source.QuantityIn,
            source.QuantityOut,
            source.BalanceAfter,
            source.UnitCost,
            source.AverageCostAfter,
            source.InventoryValueAfter,
            source.MovementDate,
            source.CreatedAtUtc,
            source.CreatedBy);
    }
}
