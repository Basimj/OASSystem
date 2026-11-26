using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Client.Accounting.Pages;

public static class IncomeStatementPresentation
{
    public sealed record Totals(decimal Revenue, decimal Cost, decimal OperatingExpenses, decimal OtherRevenue, decimal OtherExpenses, decimal IncomeTax, decimal UnclassifiedRevenue, decimal UnclassifiedExpense)
    {
        public decimal Gross => Revenue + Cost;
        public decimal OperatingProfit => Gross + OperatingExpenses;
        public decimal BeforeTax => OperatingProfit + OtherRevenue + OtherExpenses + UnclassifiedRevenue + UnclassifiedExpense;
        public decimal Net => BeforeTax + IncomeTax;
        public decimal? Margin => Revenue > 0 ? Net / Revenue * 100m : null;
    }
    // All values are signed credit minus debit, including expense reversals and contra-revenue accounts.
    public static Totals Sum(IEnumerable<IncomeStatementAccountDto> accounts, IEnumerable<IncomeStatementMovementDto> movements)
    {
        var metadata = accounts.ToDictionary(a => a.AccountId);
        var rows = movements.Select(m => (Account: metadata[m.AccountId], m.Amount)).ToArray();
        decimal Category(FinancialStatementCategory c) => rows.Where(r => r.Account.Category == c).Sum(r => r.Amount);
        return new(Category(FinancialStatementCategory.OperatingRevenue) + Category(FinancialStatementCategory.RevenueReductions),
            Category(FinancialStatementCategory.CostOfSales), Category(FinancialStatementCategory.OperatingExpenses),
            Category(FinancialStatementCategory.OtherRevenue), Category(FinancialStatementCategory.OtherExpenses), Category(FinancialStatementCategory.IncomeTax),
            rows.Where(r => r.Account.Category is null && r.Account.AccountClass == AccountClass.Revenue).Sum(r => r.Amount),
            rows.Where(r => r.Account.Category is null && r.Account.AccountClass == AccountClass.Expense).Sum(r => r.Amount));
    }
}
