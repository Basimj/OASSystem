using NUnit.Framework;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Tests.Accounting.Application.Reports;

[TestFixture]
public sealed class BalanceSheetTotalsTests
{
    private static BalanceSheetAccountDto Account(AccountClass cls, FinancialStatementCategory? category, decimal signed, Guid? parent = null)
        => new(Guid.NewGuid(), "100", "حساب", parent, (byte)(parent.HasValue ? 2 : 1), cls, category, signed);

    [Test]
    public void ClosingTransfersResultWithoutIncreasingEquityTwice()
    {
        var cash = Account(AccountClass.Asset, FinancialStatementCategory.CurrentAssets, 1400m);
        var capital = Account(AccountClass.Equity, FinancialStatementCategory.Equity, -1000m);
        var revenue = Account(AccountClass.Revenue, FinancialStatementCategory.OperatingRevenue, -1000m);
        var expense = Account(AccountClass.Expense, FinancialStatementCategory.OperatingExpenses, 600m);
        var before = BalanceSheetTotalsDto.Calculate([cash, capital, revenue, expense]);
        var after = BalanceSheetTotalsDto.Calculate([cash, capital, revenue with { DebitMinusCredit = 0m }, expense with { DebitMinusCredit = 0m },
            Account(AccountClass.Equity, FinancialStatementCategory.Equity, -400m)]);
        Assert.Multiple(() =>
        {
            Assert.That(before.UnclosedResult, Is.EqualTo(400m));
            Assert.That(after.UnclosedResult, Is.Zero);
            Assert.That(before.Equity, Is.EqualTo(1400m));
            Assert.That(after.Equity, Is.EqualTo(before.Equity));
            Assert.That(before.IsBalanced && after.IsBalanced, Is.True);
        });
    }

    [Test]
    public void ContraAssetsAndDebitEquityReduceTotals_AndDirectParentBalanceIsCountedOnce()
    {
        var parent = Account(AccountClass.Asset, FinancialStatementCategory.NonCurrentAssets, 0m);
        var rows = new[] { parent, Account(AccountClass.Asset, FinancialStatementCategory.NonCurrentAssets, 500m, parent.AccountId),
            Account(AccountClass.Asset, FinancialStatementCategory.NonCurrentAssets, -100m, parent.AccountId),
            Account(AccountClass.Equity, FinancialStatementCategory.Equity, -450m), Account(AccountClass.Equity, FinancialStatementCategory.Equity, 50m) };
        var totals = BalanceSheetTotalsDto.Calculate(rows);
        Assert.Multiple(() => { Assert.That(totals.Assets, Is.EqualTo(400m)); Assert.That(totals.Equity, Is.EqualTo(400m)); Assert.That(totals.IsBalanced, Is.True); });
    }

    [Test]
    public void UnknownMaturityIsIncludedInGrandTotals_AndWorkingCapitalIsUnavailable()
    {
        var totals = BalanceSheetTotalsDto.Calculate([
            Account(AccountClass.Asset, FinancialStatementCategory.CurrentAssets, 200m), Account(AccountClass.Asset, null, 50m),
            Account(AccountClass.Liability, null, -100m), Account(AccountClass.Equity, FinancialStatementCategory.Equity, -150m)]);
        Assert.Multiple(() => { Assert.That(totals.Assets, Is.EqualTo(250m)); Assert.That(totals.Liabilities, Is.EqualTo(100m)); Assert.That(totals.HasUnclassifiedBalances, Is.True); Assert.That(totals.WorkingCapital, Is.Null); Assert.That(totals.IsBalanced, Is.True); });
    }

    [Test]
    public void LossAndOutstandingDifferenceAreNotHidden()
    {
        var rows = new[] { Account(AccountClass.Asset, FinancialStatementCategory.CurrentAssets, 80m),
            Account(AccountClass.Equity, FinancialStatementCategory.Equity, -100m), Account(AccountClass.Expense, null, 20m) };
        var balanced = BalanceSheetTotalsDto.Calculate(rows);
        var unbalanced = BalanceSheetTotalsDto.Calculate(rows.Append(Account(AccountClass.Asset, FinancialStatementCategory.CurrentAssets, 1m)));
        Assert.Multiple(() => { Assert.That(balanced.UnclosedResult, Is.EqualTo(-20m)); Assert.That(balanced.IsBalanced, Is.True); Assert.That(unbalanced.Difference, Is.EqualTo(1m)); Assert.That(unbalanced.IsBalanced, Is.False); });
    }
}
