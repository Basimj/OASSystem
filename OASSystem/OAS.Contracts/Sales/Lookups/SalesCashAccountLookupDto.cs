namespace OAS.Contracts.Sales.Lookups;

public sealed record SalesCashAccountLookupDto(
    Guid Id,
    string Code,
    string Name,
    Guid CurrencyId,
    bool IsDefault,
    bool IsActive);
