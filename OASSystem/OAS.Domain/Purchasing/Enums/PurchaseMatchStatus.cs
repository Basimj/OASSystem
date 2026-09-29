namespace OAS.Domain.Purchasing.Enums;

public enum PurchaseMatchStatus : byte
{
    Pending = 1,
    Matched = 2,
    WithinTolerance = 3,
    RequiresApproval = 4,
    ApprovedVariance = 5,
    Rejected = 6
}
