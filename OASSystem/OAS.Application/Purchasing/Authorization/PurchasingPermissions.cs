namespace OAS.Application.Purchasing.Authorization;

public static class PurchasingPermissions
{
    public const string View = "purchasing.view";

    public static class Catalog
    {
        public const string View = "purchasing.catalog.view";
        public const string Manage = "purchasing.catalog.manage";
    }


    public static class CustomerDemand
    {
        public const string View = "purchasing.customer_demand.view";
        public const string EditSupplier = "purchasing.customer_demand.supplier.edit";
    }

    public static class Requests
    {
        public const string View = "purchasing.requests.view";
        public const string Create = "purchasing.requests.create";
        public const string Edit = "purchasing.requests.edit";
        public const string Approve = "purchasing.requests.approve";
        public const string Cancel = "purchasing.requests.cancel";
    }

    public static class Orders
    {
        public const string View = "purchasing.orders.view";
        public const string Create = "purchasing.orders.create";
        public const string Edit = "purchasing.orders.edit";
        public const string Approve = "purchasing.orders.approve";
        public const string Send = "purchasing.orders.send";
        public const string Cancel = "purchasing.orders.cancel";
        public const string Close = "purchasing.orders.close";
    }

    public static class Receipts
    {
        public const string View = "purchasing.receipts.view";
        public const string Create = "purchasing.receipts.create";
        public const string Edit = "purchasing.receipts.edit";
        public const string Confirm = "purchasing.receipts.confirm";
        public const string Post = "purchasing.receipts.post";
        public const string Cancel = "purchasing.receipts.cancel";
    }

    public static class Invoices
    {
        public const string View = "purchasing.invoices.view";
        public const string Create = "purchasing.invoices.create";
        public const string Edit = "purchasing.invoices.edit";
        public const string Match = "purchasing.invoices.match";
        public const string Post = "purchasing.invoices.post";
        public const string Cancel = "purchasing.invoices.cancel";
    }

    public static class Variance
    {
        public const string Approve = "purchasing.variance.approve";
    }
}
