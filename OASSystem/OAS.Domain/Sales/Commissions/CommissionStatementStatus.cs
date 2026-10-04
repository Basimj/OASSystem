namespace OAS.Domain.Sales.Commissions;

public enum CommissionStatementStatus : byte
{
    Draft = 1,
    Calculated = 2,
    Finalized = 3,
    Cancelled = 4
}
