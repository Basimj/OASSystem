namespace OAS.Domain.Accounting.Enums;

public enum BankStatementLineMatchStatus : byte
{
    Unmatched = 1,
    PartiallyMatched = 2,
    Matched = 3,
    Ignored = 4
}
