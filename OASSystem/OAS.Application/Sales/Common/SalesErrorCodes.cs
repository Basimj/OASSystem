namespace OAS.Application.Sales.Common;

public static class SalesErrorCodes
{
    public const string InvoiceNotFound = "sales_invoice_not_found";
    public const string InvoiceInvalidStatus = "sales_invoice_invalid_status";
    public const string InvoiceAlreadyPosted = "sales_invoice_already_posted";
    public const string InvoicePeriodNotFound = "sales_invoice_period_not_found";
    public const string InvoicePeriodClosed = "sales_invoice_period_closed";
    public const string CustomerInactive = "sales_customer_inactive";
    public const string CustomerAccountInvalid = "sales_customer_account_invalid";
    public const string InsufficientStock = "sales_insufficient_stock";
    public const string ReservationConflict = "sales_reservation_conflict";
    public const string PriceOverrideRequired = "sales_price_override_required";
    public const string PriceOverrideNotApproved = "sales_price_override_not_approved";
    public const string CreditNotAllowed = "sales_credit_not_allowed";
    public const string CreditLimitExceeded = "sales_credit_limit_exceeded";
    public const string PrescriptionRequired = "sales_prescription_required";
    public const string PrescriptionRevisionRequired = "sales_prescription_revision_required";
    public const string PrescriptionEyeRequired = "sales_prescription_eye_required";
    public const string PrescriptionEyeDuplicate = "sales_prescription_eye_duplicate";
    public const string InvalidPrescriptionRevision = "sales_invalid_prescription_revision";
    public const string DuplicateInvoiceCode = "sales_duplicate_invoice_code";
    public const string DuplicateOrderCode = "sales_duplicate_order_code";
    public const string DuplicatePrescriptionCode = "sales_duplicate_prescription_code";
}
