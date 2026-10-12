namespace OAS.Contracts.Inventory.Products;
public sealed record UpdateLensDetailsRequest(
    string LensType,
    string? Material,
    string? Coating,
    decimal? RefractiveIndex,
    decimal? SPH,
    decimal? CYL,
    decimal? ADD,
    decimal? BaseCurve,
    decimal? Diameter,
    bool IsPrescriptionLens,
    bool IsActive = true);
