namespace OAS.Contracts.Purchasing.Enums;

public enum PurchaseOrderStatus : byte
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Sent = 4,
    PartiallyReceived = 5,
    FullyReceived = 6,
    Closed = 7,
    Rejected = 8,
    Cancelled = 9
}
