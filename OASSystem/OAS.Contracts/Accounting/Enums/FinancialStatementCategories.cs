using OAS.Contracts.Accounting.Accounts;
namespace OAS.Contracts.Accounting.Enums;

/// <summary>Null means inherit from the nearest compatible ancestor; equity has one implicit category.</summary>
public static class FinancialStatementCategories
{
    public static bool IsCompatible(AccountClass accountClass, FinancialStatementCategory category) => accountClass switch
    {
        AccountClass.Asset => category is FinancialStatementCategory.CurrentAssets or FinancialStatementCategory.NonCurrentAssets,
        AccountClass.Liability => category is FinancialStatementCategory.CurrentLiabilities or FinancialStatementCategory.NonCurrentLiabilities,
        AccountClass.Equity => category == FinancialStatementCategory.Equity,
        AccountClass.Revenue => category is FinancialStatementCategory.OperatingRevenue or FinancialStatementCategory.RevenueReductions or FinancialStatementCategory.OtherRevenue,
        AccountClass.Expense => category is FinancialStatementCategory.CostOfSales or FinancialStatementCategory.OperatingExpenses or FinancialStatementCategory.OtherExpenses or FinancialStatementCategory.IncomeTax,
        _ => false
    };

    public static FinancialStatementCategory? Resolve(AccountClass accountClass, FinancialStatementCategory? own,
        Guid? parentId, IReadOnlyDictionary<Guid, AccountDto> accounts)
    {
        if (own.HasValue && IsCompatible(accountClass, own.Value)) return own;
        if (accountClass == AccountClass.Equity) return FinancialStatementCategory.Equity;
        var visited = new HashSet<Guid>();
        while (parentId is Guid id && visited.Add(id) && accounts.TryGetValue(id, out var parent))
        {
            // Never inherit a category across a different accounting class.
            if (parent.AccountClass != accountClass) break;
            if (parent.FinancialStatementCategory is { } category && IsCompatible(accountClass, category)) return category;
            parentId = parent.ParentAccountId;
        }
        return null;
    }
}
