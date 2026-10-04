using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Entities.Inventory;

public sealed class LensVariantDetail : AuditableEntity<Guid>
{
    private LensVariantDetail() { }

    private LensVariantDetail(
        Guid id,
        Guid productVariantId,
        decimal? sph,
        decimal? cyl,
        decimal? add,
        decimal? baseCurve,
        decimal? diameter,
        bool isActive)
    {
        if (id == Guid.Empty)
            throw new DomainException("Lens variant detail id is required.");
        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant id is required.");

        Id = id;
        ProductVariantId = productVariantId;
        ApplyOpticalIdentity(sph, cyl, add, baseCurve, diameter);
        IsActive = isActive;
    }

    public Guid ProductVariantId { get; private set; }
    public decimal? SPH { get; private set; }
    public decimal? CYL { get; private set; }
    public decimal? ADD { get; private set; }
    public decimal? BaseCurve { get; private set; }
    public decimal? Diameter { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static LensVariantDetail Create(
        Guid id,
        Guid productVariantId,
        decimal? sph = null,
        decimal? cyl = null,
        decimal? add = null,
        decimal? baseCurve = null,
        decimal? diameter = null,
        bool isActive = true) =>
        new(id, productVariantId, sph, cyl, add, baseCurve, diameter, isActive);

    public void Update(
        decimal? sph,
        decimal? cyl,
        decimal? add,
        decimal? baseCurve,
        decimal? diameter,
        bool isActive)
    {
        ApplyOpticalIdentity(sph, cyl, add, baseCurve, diameter);
        IsActive = isActive;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    public bool Matches(decimal? sph, decimal? cyl, decimal? add) =>
        IsActive && SPH == sph && CYL == cyl && ADD == add;

    private void ApplyOpticalIdentity(
        decimal? sph,
        decimal? cyl,
        decimal? add,
        decimal? baseCurve,
        decimal? diameter)
    {
        if (add is < 0)
            throw new DomainException("ADD cannot be negative.");
        if (baseCurve.HasValue && baseCurve.Value <= 0)
            throw new DomainException("Base curve must be greater than zero when provided.");
        if (diameter.HasValue && diameter.Value <= 0)
            throw new DomainException("Diameter must be greater than zero when provided.");

        SPH = sph;
        CYL = cyl;
        ADD = add;
        BaseCurve = baseCurve;
        Diameter = diameter;
    }
}
