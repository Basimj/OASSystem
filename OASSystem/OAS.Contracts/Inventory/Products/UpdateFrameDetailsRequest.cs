namespace OAS.Contracts.Inventory.Products;

public sealed record UpdateFrameDetailsRequest(
    string Model,
    string? Material,
    string? RimType,
    string? Gender,
    string? Shape,
    decimal? TempleLength,
    decimal? BridgeSize,
    decimal? LensWidth);