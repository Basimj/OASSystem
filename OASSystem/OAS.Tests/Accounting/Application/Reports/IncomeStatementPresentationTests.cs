using NUnit.Framework;
using OAS.Client.Accounting.Pages;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.Reports;
namespace OAS.Tests.Accounting.Application.Reports;
[TestFixture]
public sealed class IncomeStatementPresentationTests
{
    private static IncomeStatementAccountDto Account(AccountClass cls, FinancialStatementCategory? cat) => new(Guid.NewGuid(), "100", "حساب", null, 1, cls, cat);
    [Test]
    public void SignedMovementsCalculateNetAndUnclassifiedWithoutDoubleCounting()
    {
        var revenue = Account(AccountClass.Revenue, FinancialStatementCategory.OperatingRevenue);
        var reduction = Account(AccountClass.Revenue, FinancialStatementCategory.RevenueReductions);
        var cost = Account(AccountClass.Expense, FinancialStatementCategory.CostOfSales);
        var expenses = Account(AccountClass.Expense, FinancialStatementCategory.OperatingExpenses);
        var unknown = Account(AccountClass.Expense, null);
        var tax = Account(AccountClass.Expense, FinancialStatementCategory.IncomeTax);
        var totals = IncomeStatementPresentation.Sum([revenue, reduction, cost, expenses, unknown, tax],
            [new(revenue.AccountId, 2026, 1, 1000), new(revenue.AccountId, 2026, 2, -100), new(reduction.AccountId, 2026, 2, -50), new(cost.AccountId, 2026, 1, -400), new(expenses.AccountId, 2026, 1, -200), new(unknown.AccountId, 2026, 1, -25), new(tax.AccountId, 2026, 1, -25)]);
        Assert.Multiple(() => { Assert.That(totals.Revenue, Is.EqualTo(850)); Assert.That(totals.Gross, Is.EqualTo(450)); Assert.That(totals.OperatingProfit, Is.EqualTo(250)); Assert.That(totals.Net, Is.EqualTo(200)); });
    }
    [Test]
    public void ZeroRevenueDoesNotProduceMisleadingMargin()
    {
        var expenses=Account(AccountClass.Expense, FinancialStatementCategory.OperatingExpenses);
        var totals=IncomeStatementPresentation.Sum([expenses],[new(expenses.AccountId,2026,1,-20)]);
        Assert.That(totals.Net,Is.EqualTo(-20)); Assert.That(totals.Margin,Is.Null);
    }
}
