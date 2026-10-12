using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Entities.Inventory;

/// <summary>
/// Exact optical identity for a lens SKU. One ProductVariant can have at most one LensDetails row.
/// </summary>
public sealed class LensDetails : AuditableEntity<Guid>
{
    public Guid ProductVariantId { get; private set; }
    public string LensType { get; private set; } = null!;
    public string? Material { get; private set; }
    public string? Coating { get; private set; }
    public decimal? RefractiveIndex { get; private set; }
    public decimal? SPH { get; private set; }
    public decimal? CYL { get; private set; }
    public decimal? ADD { get; private set; }
    public decimal? BaseCurve { get; private set; }
    public decimal? Diameter { get; private set; }
    public bool IsPrescriptionLens { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private LensDetails() { }

    public LensDetails(
        Guid productVariantId,
        string lensType,
        bool isPrescriptionLens = false,
        string? material = null,
        string? coating = null,
        decimal? refractiveIndex = null,
        decimal? sph = null,
        decimal? cyl = null,
        decimal? add = null,
        decimal? baseCurve = null,
        decimal? diameter = null,
        bool isActive = true)
    {
        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant id is required.");
        if (string.IsNullOrWhiteSpace(lensType))
            throw new DomainException("Lens type is required.");

        Id = Guid.NewGuid();
        ProductVariantId = productVariantId;
        Apply(lensType, isPrescriptionLens, material, coating, refractiveIndex, sph, cyl, add, baseCurve, diameter, isActive);
    }

    public void UpdateDetails(
        string lensType,
        bool isPrescriptionLens = false,
        string? material = null,
        string? coating = null,
        decimal? refractiveIndex = null,
        decimal? sph = null,
        decimal? cyl = null,
        decimal? add = null,
        decimal? baseCurve = null,
        decimal? diameter = null,
        bool isActive = true)
        => Apply(lensType, isPrescriptionLens, material, coating, refractiveIndex, sph, cyl, add, baseCurve, diameter, isActive);

    public bool Matches(decimal? sph, decimal? cyl, decimal? add) =>
        IsActive && SPH == sph && CYL == cyl && ADD == add;

    private void Apply(
        string lensType,
        bool isPrescriptionLens,
        string? material,
        string? coating,
        decimal? refractiveIndex,
        decimal? sph,
        decimal? cyl,
        decimal? add,
        decimal? baseCurve,
        decimal? diameter,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(lensType))
            throw new DomainException("Lens type is required.");
        if (refractiveIndex.HasValue && refractiveIndex.Value <= 0m)
            throw new DomainException("Refractive index must be greater than zero when provided.");
        if (add is < 0m)
            throw new DomainException("ADD cannot be negative.");
        if (baseCurve.HasValue && baseCurve.Value <= 0m)
            throw new DomainException("Base curve must be greater than zero when provided.");
        if (diameter.HasValue && diameter.Value <= 0m)
            throw new DomainException("Diameter must be greater than zero when provided.");

        LensType = lensType.Trim();
        Material = string.IsNullOrWhiteSpace(material) ? null : material.Trim();
        Coating = string.IsNullOrWhiteSpace(coating) ? null : coating.Trim();
        RefractiveIndex = refractiveIndex;
        SPH = sph;
        CYL = cyl;
        ADD = add;
        BaseCurve = baseCurve;
        Diameter = diameter;
        IsPrescriptionLens = isPrescriptionLens;
        IsActive = isActive;
    }
}
