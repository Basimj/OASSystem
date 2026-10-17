namespace OAS.Domain.Sales.Commissions;

public enum CommissionStatementStatus : byte
{
    Draft = 1,
    Calculated = 2,
    // Legacy closed state. Numeric value is retained for existing rows.
    Finalized = 3,
    Cancelled = 4,
    Reviewed = 5,
    Approved = 6,
    Settled = 7
}
