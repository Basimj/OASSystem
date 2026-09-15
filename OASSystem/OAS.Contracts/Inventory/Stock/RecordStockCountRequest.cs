namespace OAS.Contracts.Inventory.Stock;

public sealed record RecordStockCountRequest(
    decimal CountedQuantity,
    DateTimeOffset CountedAtUtc,
    string? CountedBy);