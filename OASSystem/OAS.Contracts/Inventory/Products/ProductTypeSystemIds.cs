namespace OAS.Contracts.Inventory.Products;

public static class ProductTypeSystemKeys
{
    public const string Frame = "FRAME";
    public const string Lens = "LENS";
    public const string Sunglasses = "SUNGLASSES";
    public const string Accessory = "ACCESSORY";
    public const string Other = "OTHER";
    public const string Service = "SERVICE";
}

public static class ProductTypeSystemIds
{
    public static readonly Guid Frame = Guid.Parse("71000000-0000-0000-0000-000000000001");
    public static readonly Guid Lens = Guid.Parse("71000000-0000-0000-0000-000000000002");
    public static readonly Guid Sunglasses = Guid.Parse("71000000-0000-0000-0000-000000000003");
    public static readonly Guid Accessory = Guid.Parse("71000000-0000-0000-0000-000000000004");
    public static readonly Guid Other = Guid.Parse("71000000-0000-0000-0000-000000000005");
    public static readonly Guid Service = Guid.Parse("71000000-0000-0000-0000-000000000006");
}
