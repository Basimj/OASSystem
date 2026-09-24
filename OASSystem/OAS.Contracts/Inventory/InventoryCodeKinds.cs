namespace OAS.Contracts.Inventory;

public static class InventoryCodeKinds
{
    public const string Product = "product";
    public const string Brand = "brand";
    public const string ProductCategory = "product-category";
    public const string ProductType = "product-type";
    public const string Unit = "unit";
    public const string Warehouse = "warehouse";
    public const string ProductVariant = "product-variant";
}

public sealed record InventoryCodeSuggestionDto(string Kind, string Code);
