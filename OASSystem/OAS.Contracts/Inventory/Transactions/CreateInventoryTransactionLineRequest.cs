namespace OAS.Contracts.Inventory.Transactions;

public sealed record CreateInventoryTransactionLineRequest(
    Guid ProductVariantId,
    decimal Quantity,
    decimal UnitCost,
    string? Notes);