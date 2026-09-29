namespace OAS.Contracts.Purchasing.SupplierCatalog;

public sealed record CloseSupplierPriceRequest(
    DateOnly EffectiveTo,
    string RowVersion);
