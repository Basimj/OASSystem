namespace OAS.Domain.Accounting.Enums;

public enum BankReconciliationStatus : byte
{
    Draft = 1,
    InReview = 2,
    Approved = 3,
    Reconciled = 4,
    Cancelled = 5
}
