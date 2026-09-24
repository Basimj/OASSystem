namespace OAS.Contracts.Inventory.Products;

public sealed record CreateStockProductRequest(
    CreateProductRequest Product,
    InitialProductVariantRequest? Variant,
    OpeningInventoryRequest? OpeningInventory,
    InitialFrameDetailsRequest? FrameDetails = null,
    InitialLensDetailsRequest? LensDetails = null);
