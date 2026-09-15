using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class FrameDetails : Entity<Guid>
{
    public Guid ProductId { get; private set; }

    public string Model { get; private set; } = null!;
    public string? Material { get; private set; }
    public string? RimType { get; private set; }
    public string? Gender { get; private set; }
    public string? Shape { get; private set; }

    public decimal? TempleLength { get; private set; }
    public decimal? BridgeSize { get; private set; }
    public decimal? LensWidth { get; private set; }

    private FrameDetails()
    {
    }

    public FrameDetails(
        Guid productId,
        string model,
        string? material = null,
        string? rimType = null,
        string? gender = null,
        string? shape = null,
        decimal? templeLength = null,
        decimal? bridgeSize = null,
        decimal? lensWidth = null)
    {
        Id = Guid.NewGuid();

        ProductId = productId;
        Model = model;
        Material = material;
        RimType = rimType;
        Gender = gender;
        Shape = shape;
        TempleLength = templeLength;
        BridgeSize = bridgeSize;
        LensWidth = lensWidth;
    }
}