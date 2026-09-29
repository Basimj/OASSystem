namespace OAS.Contracts.Sales.Lookups;

public sealed record SalesCurrencyLookupDto(
    Guid Id,
    string Code,
    string NameAr,
    string? Symbol,
    byte DecimalPlaces,
    decimal EffectiveExchangeRate,
    DateOnly EffectiveRateDate,
    bool IsBaseCurrency,
    bool IsActive);
