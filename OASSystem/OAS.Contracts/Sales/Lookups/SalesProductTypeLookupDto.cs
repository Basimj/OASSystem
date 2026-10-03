using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.Lookups;

public sealed record SalesProductTypeLookupDto(
    Guid Id,
    string Code,
    string NameAr,
    string? SystemKey,
    SalesLineType SalesLineType,
    bool IsActive);
