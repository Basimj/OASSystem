namespace OAS.Contracts.Purchasing.SupplierCatalog;

public sealed record SupplierPriceHistoryDto(
    Guid Id,
    Guid SupplierCatalogItemId,
    Guid CurrencyId,
    string? CurrencyCode,
    decimal UnitPrice,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsCurrent,
    string? Notes,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedAtUtc,
    string? LastModifiedBy);
