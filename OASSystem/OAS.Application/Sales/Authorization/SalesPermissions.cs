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


    public static class Checkout
    {
        public const string Create = "sales.checkout.create";
        public const string Confirm = "sales.checkout.confirm";
        public const string Deliver = "sales.checkout.deliver";
    }

    public static class OrderOperations
    {
        public const string View = "sales.order_operations.view";
        public const string ViewAll = "sales.order_operations.view_all";
    }

    public static class Credit
    {
        public const string Use = "sales.credit.use";
    }

    public const string ViewPrescription = "sales.prescription.view";
    public const string UseManualOpticalMeasurements = "sales.optical_measurements.manual.use";

    public static class OpticalJobs
    {
        public const string View = "optical_jobs.view";
        public const string ViewAll = "optical_jobs.view_all";
        public const string Assign = "optical_jobs.assign";
        public const string Reassign = "optical_jobs.reassign";
        public const string Start = "optical_jobs.start";
        public const string IssueMaterials = "optical_jobs.issue_materials";
        public const string QC = "optical_jobs.qc";
        public const string RecordBreakage = "optical_jobs.record_breakage";
        public const string Remake = "optical_jobs.remake";
        public const string MarkReady = "optical_jobs.mark_ready";
        public const string Deliver = "optical_jobs.deliver";
        public const string Cancel = "optical_jobs.cancel";
        public const string Override = "optical_jobs.override";
        // Backward-compatible alias used by older policies/tests.
        public const string Complete = QC;
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
