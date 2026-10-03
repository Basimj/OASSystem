namespace OAS.Contracts.Sales.Lookups;

public sealed record SalesProductCategoryLookupDto(
    Guid Id,
    string Code,
    string NameAr,
    Guid? ParentCategoryId,
    bool IsActive);
