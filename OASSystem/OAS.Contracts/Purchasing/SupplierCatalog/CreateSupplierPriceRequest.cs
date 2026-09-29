namespace OAS.Contracts.Purchasing.SupplierCatalog;

public sealed record CreateSupplierPriceRequest(
    Guid CurrencyId,
    decimal UnitPrice,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsCurrent,
    string? Notes);
