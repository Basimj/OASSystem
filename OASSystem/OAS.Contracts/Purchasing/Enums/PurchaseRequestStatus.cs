namespace OAS.Contracts.Purchasing.Enums;

public enum PurchaseRequestStatus : byte
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    PartiallyConverted = 4,
    Converted = 5,
    Rejected = 6,
    Cancelled = 7
}
