namespace OAS.Contracts.Inventory.Products;

public sealed record CreateLensDetailsRequest(
    Guid ProductId,
    string LensType,
    string? Material,
    string? Coating,
    decimal? RefractiveIndex,
    decimal? SphereMin,
    decimal? SphereMax,
    decimal? CylinderMin,
    decimal? CylinderMax,
    decimal? AddMin,
    decimal? AddMax,
    bool IsPrescriptionLens);