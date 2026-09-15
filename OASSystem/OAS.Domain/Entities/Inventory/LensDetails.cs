using OAS.Domain.Common.Entities;

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
        RefractiveIndex = refractiveIndex;
        SphereMin = sphereMin;
        SphereMax = sphereMax;
        CylinderMin = cylinderMin;
        CylinderMax = cylinderMax;
        AddMin = addMin;
        AddMax = addMax;
        IsPrescriptionLens = isPrescriptionLens;
    }
}