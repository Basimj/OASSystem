namespace OAS.Contracts.Sales.Lookups;

public sealed record SalesProductCategoryLookupDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);
