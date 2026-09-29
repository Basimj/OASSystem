namespace OAS.Domain.Purchasing.Enums;

public enum PurchaseInvoiceStatus : byte
{
    Draft = 1,
    Confirmed = 2,
    PendingMatchApproval = 3,
    Posted = 4,
    Cancelled = 5
}
