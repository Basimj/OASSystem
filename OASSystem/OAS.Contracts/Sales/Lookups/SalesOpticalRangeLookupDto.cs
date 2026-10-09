namespace OAS.Contracts.Sales.Lookups;

public sealed record SalesOpticalRangeLookupDto(
    decimal? SphereMin,
    decimal? SphereMax,
    decimal? CylinderMin,
    decimal? CylinderMax,
    decimal? AddMin,
    decimal? AddMax,
    int ConfiguredLensCount);
