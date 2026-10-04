namespace OAS.Domain.Accounting.Enums;

public enum CustomerAdvanceStatus : byte
{
    Available = 1,
    PartiallyApplied = 2,
    Applied = 3,
    Refunded = 4,
    Cancelled = 5
}
