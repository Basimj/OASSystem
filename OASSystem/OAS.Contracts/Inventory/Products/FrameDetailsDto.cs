namespace OAS.Contracts.Inventory.Products;

public sealed record FrameDetailsDto(
    Guid Id,
    Guid ProductId,
    string Model,
    string? Material,
    string? RimType,
    string? Gender,
    string? Shape,
    decimal? TempleLength,
    decimal? BridgeSize,
    decimal? LensWidth);