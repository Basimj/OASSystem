namespace OAS.Contracts.Accounting.Reports;

public sealed record CashBankReportFilterDto(DateOnly FromDate, DateOnly ToDate, string Mode = "balances",
    string Kind = "all", Guid? AccountId = null, Guid? CurrencyId = null, string Activity = "all",
    bool IncludeZero = false, string ShiftState = "all", string SettlementState = "all", Guid? OpenedBy = null, byte? MovementType = null);
public sealed record CashBankReportAccountDto(Guid AccountId, string Code, string Name, string Kind, Guid? CurrencyId, bool IsActive, byte? MovementType = null);
public sealed record CashBankReportOperatorDto(Guid Id, string Name);
public sealed record CashBankReportCurrencyDto(Guid Id, string Code, string Name, byte DecimalPlaces);
// Amounts are numeric, never formatted on the server. Only Base cells may be converted for display.
public sealed record CashBankReportCell(string? Text = null, decimal? Amount = null, string Format = "text", Guid? LinkId = null, string? LinkKind = null);
public sealed record CashBankReportRow(IReadOnlyList<CashBankReportCell> Cells);
public sealed record CashBankReportMetric(string Label, CashBankReportCell Value);
public sealed record CashBankReportDto(Guid BaseCurrencyId, string BaseCurrencyCode, byte BaseDecimalPlaces,
    IReadOnlyList<CashBankReportAccountDto> Accounts, IReadOnlyList<CashBankReportCurrencyDto> Currencies,
    IReadOnlyList<string> Headers, IReadOnlyList<CashBankReportRow> Rows, IReadOnlyList<CashBankReportMetric> Metrics,
    IReadOnlyList<string> Warnings)
{
    public IReadOnlyList<CashBankReportOperatorDto> Operators { get; init; } = [];
    public IReadOnlyList<CashBankReportAccountDto> SettlementAccounts { get; init; } = [];
    public CashBankReportRow? TotalRow { get; init; }
}

public static class CashBankReportMath
{
    // Unknown amounts must propagate; a partial subtotal must never look like a complete balance.
    public static decimal? Sum(IEnumerable<decimal?> values)
    {
        decimal total = 0;
        foreach (var value in values) { if (!value.HasValue) return null; total += value.Value; }
        return total;
    }
    public static (decimal? Debit, decimal? Credit) Native(Guid? accountCurrency, Guid? journalBase, Guid? transactionCurrency,
        decimal? transactionDebit, decimal? transactionCredit, decimal debit, decimal credit)
    {
        if (!accountCurrency.HasValue) return (null, null);
        if (transactionCurrency == accountCurrency && transactionDebit.HasValue && transactionCredit.HasValue)
            return (transactionDebit, transactionCredit);
        if (transactionCurrency is null && journalBase == accountCurrency) return (debit, credit);
        return (null, null);
    }
}
