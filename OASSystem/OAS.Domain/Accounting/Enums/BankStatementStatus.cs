namespace OAS.Domain.Accounting.Enums;

public enum BankStatementStatus : byte
{
    Draft = 1,
    Imported = 2,
    Reviewed = 3,
    Reconciled = 4,
    Cancelled = 5
}
