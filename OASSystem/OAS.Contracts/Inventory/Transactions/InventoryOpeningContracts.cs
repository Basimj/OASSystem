namespace OAS.Contracts.Inventory.Transactions;

public sealed record InventoryOpeningRequest(Guid WarehouseId, DateOnly OpeningDate, string? Notes,
    IReadOnlyList<CreateInventoryTransactionLineRequest> Lines);
public sealed record InventoryOpeningInfo(bool Enabled, string CurrencyName, string CurrencyCode, IReadOnlyList<Guid> ReversedIds);
public sealed record ReverseInventoryOpeningRequest(DateOnly PostingDate, string Reason);
public sealed record InventoryOpeningImportRow(string SKU, decimal Quantity, decimal UnitCost, string? Notes);
