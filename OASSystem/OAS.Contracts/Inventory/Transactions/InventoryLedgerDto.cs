using OAS.Contracts.Enums.Inventory;
using OAS.Contracts.Inventory;

namespace OAS.Contracts.Inventory.Transactions;

public sealed record InventoryLedgerDto(
    Guid Id,
    long SequenceNumber,
    Guid TransactionId,
    Guid TransactionLineId,
    Guid WarehouseId,
    Guid ProductVariantId,
    InventoryMovementType MovementType,
    decimal QuantityIn,
    decimal QuantityOut,
    decimal BalanceAfter,
    decimal UnitCost,
    decimal AverageCostAfter,
    decimal InventoryValueAfter,
    DateTimeOffset MovementDate,
    DateTimeOffset CreatedAtUtc,
    string CreatedBy);