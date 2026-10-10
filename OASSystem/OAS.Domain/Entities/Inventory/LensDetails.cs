using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Entities.Inventory;

public class LensDetails : Entity<Guid>
{
    public Guid ProductId { get; private set; }

    public string LensType { get; private set; } = null!;
    public string? Material { get; private set; }
    public string? Coating { get; private set; }

    public decimal? RefractiveIndex { get; private set; }

    public decimal? SphereMin { get; private set; }
    public decimal? SphereMax { get; private set; }

    public decimal? CylinderMin { get; private set; }
    public decimal? CylinderMax { get; private set; }

    public decimal? AddMin { get; private set; }
    public decimal? AddMax { get; private set; }

    public bool IsPrescriptionLens { get; private set; }

    private LensDetails()
    {
    }

    public LensDetails(
        Guid productId,
        string lensType,
        bool isPrescriptionLens = false,
        string? material = null,
        string? coating = null,
        decimal? refractiveIndex = null,
        decimal? sphereMin = null,
        decimal? sphereMax = null,
        decimal? cylinderMin = null,
        decimal? cylinderMax = null,
        decimal? addMin = null,
        decimal? addMax = null)
    {
        Id = Guid.NewGuid();

        ProductId = productId;
        LensType = lensType;
        Material = material;
        Coating = coating;
        RefractiveIndex = ValidateRefractiveIndex(refractiveIndex);
        (SphereMin, SphereMax) = NormalizeRange(sphereMin, sphereMax);
        (CylinderMin, CylinderMax) = NormalizeRange(cylinderMin, cylinderMax);
        (AddMin, AddMax) = NormalizeRange(addMin, addMax);
        IsPrescriptionLens = isPrescriptionLens;
    }

    public void UpdateDetails(
        string lensType,
        bool isPrescriptionLens = false,
        string? material = null,
        string? coating = null,
        decimal? refractiveIndex = null,
        decimal? sphereMin = null,
        decimal? sphereMax = null,
        decimal? cylinderMin = null,
        decimal? cylinderMax = null,
        decimal? addMin = null,
        decimal? addMax = null)
    {
        LensType = lensType;
        Material = material;
        Coating = coating;
        RefractiveIndex = ValidateRefractiveIndex(refractiveIndex);
        (SphereMin, SphereMax) = NormalizeRange(sphereMin, sphereMax);
        (CylinderMin, CylinderMax) = NormalizeRange(cylinderMin, cylinderMax);
        (AddMin, AddMax) = NormalizeRange(addMin, addMax);
        IsPrescriptionLens = isPrescriptionLens;
    }

    private static decimal? ValidateRefractiveIndex(decimal? value)
    {
        if (value.HasValue && value.Value <= 0m)
            throw new DomainException("Refractive index must be greater than zero when provided.");

        return value;
    }

    private static (decimal? Minimum, decimal? Maximum) NormalizeRange(decimal? first, decimal? second)
    {
        if (!first.HasValue || !second.HasValue)
            return (first, second);

        return first.Value <= second.Value
            ? (first, second)
            : (second, first);
    }
}
