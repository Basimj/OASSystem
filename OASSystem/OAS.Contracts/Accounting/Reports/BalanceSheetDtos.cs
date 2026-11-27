using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.Reports;

public sealed record BalanceSheetFilterDto(DateOnly AsOfDate, Guid? FiscalYearId = null);

// Direct ledger balance only: no descendant amounts are included in an account's balance.
public sealed record BalanceSheetAccountDto(Guid AccountId, string Code, string Name,
    Guid? ParentAccountId, byte Level, AccountClass AccountClass,
    FinancialStatementCategory? Category, decimal DebitMinusCredit)
{
    public decimal Amount => AccountClass == OAS.Contracts.Accounting.Enums.AccountClass.Asset ? DebitMinusCredit : -DebitMinusCredit;
}

public sealed record BalanceSheetTotalsDto(decimal CurrentAssets, decimal NonCurrentAssets,
    decimal UnclassifiedAssets, decimal CurrentLiabilities, decimal NonCurrentLiabilities,
    decimal UnclassifiedLiabilities, decimal RecordedEquity, decimal UnclosedResult)
{
    public decimal Assets => CurrentAssets + NonCurrentAssets + UnclassifiedAssets;
    public decimal Liabilities => CurrentLiabilities + NonCurrentLiabilities + UnclassifiedLiabilities;
    public decimal Equity => RecordedEquity + UnclosedResult;
    public decimal LiabilitiesAndEquity => Liabilities + Equity;
    public decimal Difference => Assets - LiabilitiesAndEquity;
    public bool IsBalanced => Difference == 0m;
    public bool HasUnclassifiedBalances { get; init; }
    // Unknown maturity makes working capital indeterminate, even if unknown balances net to zero.
    public decimal? WorkingCapital => HasUnclassifiedBalances ? null : CurrentAssets - CurrentLiabilities;

    public static BalanceSheetTotalsDto Calculate(IEnumerable<BalanceSheetAccountDto> accounts)
    {
        var rows = accounts.ToArray();
        decimal Sum(AccountClass cls, FinancialStatementCategory? category) => rows
            .Where(a => a.AccountClass == cls && a.Category == category).Sum(a => a.Amount);
        bool Unknown(BalanceSheetAccountDto a) => a.AccountClass switch
        {
            AccountClass.Asset => a.Category is not (FinancialStatementCategory.CurrentAssets or FinancialStatementCategory.NonCurrentAssets),
            AccountClass.Liability => a.Category is not (FinancialStatementCategory.CurrentLiabilities or FinancialStatementCategory.NonCurrentLiabilities),
            _ => false
        };
        return new(Sum(AccountClass.Asset, FinancialStatementCategory.CurrentAssets),
            Sum(AccountClass.Asset, FinancialStatementCategory.NonCurrentAssets),
            rows.Where(a => a.AccountClass == AccountClass.Asset && Unknown(a)).Sum(a => a.Amount),
            Sum(AccountClass.Liability, FinancialStatementCategory.CurrentLiabilities),
            Sum(AccountClass.Liability, FinancialStatementCategory.NonCurrentLiabilities),
            rows.Where(a => a.AccountClass == AccountClass.Liability && Unknown(a)).Sum(a => a.Amount),
            rows.Where(a => a.AccountClass == AccountClass.Equity).Sum(a => a.Amount),
            rows.Where(a => a.AccountClass is AccountClass.Revenue or AccountClass.Expense).Sum(a => a.Amount))
            { HasUnclassifiedBalances = rows.Any(a => Unknown(a) && a.DebitMinusCredit != 0m) };
    }
}

public sealed record BalanceSheetReportDto(DateOnly AsOfDate, IReadOnlyList<BalanceSheetAccountDto> Accounts,
    BalanceSheetTotalsDto Totals);
