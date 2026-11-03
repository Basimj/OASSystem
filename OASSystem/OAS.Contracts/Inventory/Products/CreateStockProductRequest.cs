namespace OAS.Contracts.Inventory.Products;

public sealed record CreateStockProductRequest(
    CreateProductRequest Product,
    InitialProductVariantRequest? Variant,
    OpeningInventoryRequest? OpeningInventory = null,
    InitialFrameDetailsRequest? FrameDetails = null,
    InitialLensDetailsRequest? LensDetails = null);
