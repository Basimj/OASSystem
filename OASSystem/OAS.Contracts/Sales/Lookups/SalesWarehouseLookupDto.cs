namespace OAS.Contracts.Sales.Lookups;

public sealed record SalesWarehouseLookupDto(
    Guid Id,
    string Code,
    string Name,
    bool IsDefault,
    bool IsActive,
    decimal? OnHandQuantity = null,
    decimal? ReservedQuantity = null,
    decimal? AvailableQuantity = null);
