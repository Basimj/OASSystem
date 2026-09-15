namespace OAS.Contracts.Inventory.Transactions;

public sealed record InventoryTransactionLineDto(
    Guid Id,
    Guid TransactionId,
    Guid ProductVariantId,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost,
    string? Notes);