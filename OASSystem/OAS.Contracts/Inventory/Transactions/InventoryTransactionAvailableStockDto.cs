namespace OAS.Contracts.Inventory.Transactions;

public sealed record InventoryTransactionAvailableStockDto(Guid ProductVariantId, decimal AvailableQuantity);
