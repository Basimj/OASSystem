namespace OAS.Contracts.Inventory.Stock;

public sealed record StockCountLineDto(
    Guid Id,
    Guid StockCountId,
    Guid ProductVariantId,
    decimal SystemQuantity,
    decimal CountedQuantity,
    decimal DifferenceQuantity,
    decimal AverageCostSnapshot,
    decimal VarianceValue,
    DateTimeOffset? CountedAtUtc,
    string? CountedBy,
    string? Notes);