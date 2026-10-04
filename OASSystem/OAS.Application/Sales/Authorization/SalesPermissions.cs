namespace OAS.Application.Sales.Authorization;

public static class SalesPermissions
{
    public const string View = "sales.view";
    public const string Create = "sales.create";
    public const string Edit = "sales.edit";
    public const string Confirm = "sales.confirm";
    public const string Post = "sales.post";
    public const string Cancel = "sales.cancel";
    public const string Discount = "sales.discount";
    public const string DiscountOverride = "sales.discount.override";
    public const string PriceOverrideRequest = "sales.price.override.request";
    public const string PriceOverrideApprove = "sales.price.override.approve";

    public const string ViewCustomerPrescriptionContext = "sales.customer_prescription_context.view";
    public const string ViewStockAvailability = "sales.stock_availability.view";

    public static class OpticalJobs
    {
        public const string View = "optical_jobs.view";
        public const string Start = "optical_jobs.start";
        public const string Assign = "optical_jobs.assign";
        public const string Complete = "optical_jobs.complete";
    }

    public static class Prescriptions
    {
        public const string View = "sales.prescriptions.view";
        public const string Create = "sales.prescriptions.create";
        public const string Edit = "sales.prescriptions.edit";
    }

    public static class Orders
    {
        public const string View = "sales.orders.view";
        public const string Create = "sales.orders.create";
        public const string Edit = "sales.orders.edit";
        public const string Confirm = "sales.orders.confirm";
        public const string Cancel = "sales.orders.cancel";
    }

    public static class Invoices
    {
        public const string View = "sales.view";
        public const string Create = "sales.create";
        public const string Edit = "sales.edit";
        public const string Confirm = "sales.confirm";
        public const string Post = "sales.post";
        public const string Cancel = "sales.cancel";
    }
}
