namespace OAS.Application.Inventory.Authorization;

public static class InventoryPermissions
{
    public static class ProductCategories
    {
        public const string View = "inventory.categories.view";
        public const string Create = "inventory.categories.create";
        public const string Edit = "inventory.categories.edit";
        public const string Delete = "inventory.categories.delete";
    }

    public static class Brands
    {
        public const string View = "inventory.brands.view";
        public const string Create = "inventory.brands.create";
        public const string Edit = "inventory.brands.edit";
        public const string Delete = "inventory.brands.delete";
    }

    public static class Units
    {
        public const string View = "inventory.units.view";
        public const string Create = "inventory.units.create";
        public const string Edit = "inventory.units.edit";
        public const string Delete = "inventory.units.delete";
    }

    public static class Warehouses
    {
        public const string View = "inventory.warehouses.view";
        public const string Create = "inventory.warehouses.create";
        public const string Edit = "inventory.warehouses.edit";
        public const string Delete = "inventory.warehouses.delete";
    }

    public static class Products
    {
        public const string View = "inventory.products.view";
        public const string Create = "inventory.products.create";
        public const string Edit = "inventory.products.edit";
        public const string Delete = "inventory.products.delete";
    }

    public static class ProductVariants
    {
        public const string View = "inventory.variants.view";
        public const string Create = "inventory.variants.create";
        public const string Edit = "inventory.variants.edit";
        public const string Delete = "inventory.variants.delete";
    }

    public static class FrameDetails
    {
        public const string View = "inventory.frame_details.view";
        public const string Create = "inventory.frame_details.create";
        public const string Edit = "inventory.frame_details.edit";
        public const string Delete = "inventory.frame_details.delete";
    }

    public static class LensDetails
    {
        public const string View = "inventory.lens_details.view";
        public const string Create = "inventory.lens_details.create";
        public const string Edit = "inventory.lens_details.edit";
        public const string Delete = "inventory.lens_details.delete";
    }

    public static class Balances
    {
        public const string View = "inventory.balances.view";
    }

    public static class Transactions
    {
        public const string View = "inventory.transactions.view";
        public const string Create = "inventory.transactions.create";
        public const string Post = "inventory.transactions.post";
    }

    public static class Ledger
    {
        public const string View = "inventory.ledger.view";
    }

    public static class StockCounts
    {
        public const string View = "inventory.stock_counts.view";
        public const string Create = "inventory.stock_counts.create";
        public const string Start = "inventory.stock_counts.start";
        public const string Record = "inventory.stock_counts.record";
        public const string Complete = "inventory.stock_counts.complete";
        public const string Approve = "inventory.stock_counts.approve";
        public const string Post = "inventory.stock_counts.post";
        public const string Cancel = "inventory.stock_counts.cancel";
    }
}
