namespace OAS.Application.Sales.Abstractions;

public sealed record LensVariantMatchRequest(
    Guid SeedProductVariantId,
    decimal? SPH,
    decimal? CYL,
    decimal? ADD);

public sealed record LensVariantResolution(
    Guid ProductVariantId,
    decimal? SPH,
    decimal? CYL,
    decimal? ADD);

public interface ILensVariantResolver
{
    Task<LensVariantResolution> ResolveAsync(
        LensVariantMatchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to resolve an exact stocked lens SKU for the requested optical identity.
    /// Returns null when no exact SPH/CYL/ADD identity exists for the product. Ambiguous
    /// identities remain a data-integrity error and are still rejected.
    /// </summary>
    Task<LensVariantResolution?> TryResolveExactAsync(
        LensVariantMatchRequest request,
        CancellationToken cancellationToken = default);
}
