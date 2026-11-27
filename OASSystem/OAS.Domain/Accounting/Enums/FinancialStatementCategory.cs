namespace OAS.Domain.Accounting.Enums;

public enum FinancialStatementCategory : byte
{
    CurrentAssets = 1,
    NonCurrentAssets = 2,
    CurrentLiabilities = 3,
    NonCurrentLiabilities = 4,
    Equity = 5,
    OperatingRevenue = 6,
    RevenueReductions = 7,
    OtherRevenue = 8,
    CostOfSales = 9,
    OperatingExpenses = 10,
    OtherExpenses = 11,
    IncomeTax = 12
}
