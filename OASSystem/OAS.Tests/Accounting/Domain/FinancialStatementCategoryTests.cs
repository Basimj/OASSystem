using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public class FinancialStatementCategoryTests
{
    private static Account Make(string code, AccountClass cls = AccountClass.Expense, Account? parent = null)
        => Account.Create(Guid.NewGuid(), code, code, null, parent?.Id, (byte)((parent?.Level ?? 0) + 1), cls,
            AccountType.Posting, cls is AccountClass.Asset or AccountClass.Expense ? NormalBalance.Debit : NormalBalance.Credit,
            true, false, true, false, true, null);

    [Test]
    public void Inheritance_IsDynamic_AndExplicitChildOverrideIsPreserved()
    {
        var parent = Make("5"); var middle = Make("51", parent: parent); var leaf = Make("511", parent: middle);
        var tree = new[] { parent, middle, leaf }.ToDictionary(x => x.Id);
        parent.SetFinancialStatementCategory(FinancialStatementCategory.OperatingExpenses);
        Assert.That(FinancialStatementCategories.Resolve(leaf.AccountClass, leaf.FinancialStatementCategory, leaf.ParentAccountId, tree), Is.EqualTo(FinancialStatementCategory.OperatingExpenses));
        parent.SetFinancialStatementCategory(FinancialStatementCategory.CostOfSales);
        Assert.That(FinancialStatementCategories.Resolve(leaf.AccountClass, leaf.FinancialStatementCategory, leaf.ParentAccountId, tree), Is.EqualTo(FinancialStatementCategory.CostOfSales));
        leaf.SetFinancialStatementCategory(FinancialStatementCategory.OtherExpenses);
        Assert.That(FinancialStatementCategories.Resolve(leaf.AccountClass, leaf.FinancialStatementCategory, leaf.ParentAccountId, tree), Is.EqualTo(FinancialStatementCategory.OtherExpenses));
        leaf.SetFinancialStatementCategory(null);
        Assert.That(FinancialStatementCategories.Resolve(leaf.AccountClass, leaf.FinancialStatementCategory, leaf.ParentAccountId, tree), Is.EqualTo(FinancialStatementCategory.CostOfSales));
    }

    [Test]
    public void WrongClassAndUnknownCategoryAreRejected()
    {
        var account = Make("5");
        Assert.Throws<ArgumentException>(() => account.SetFinancialStatementCategory(FinancialStatementCategory.CurrentAssets));
        Assert.Throws<ArgumentException>(() => account.SetFinancialStatementCategory((FinancialStatementCategory)255));
        Assert.That(account.FinancialStatementCategory, Is.Null);
    }

    [Test]
    public void MissingClassificationRemainsUnknown_EquityHasSingleImplicitCategory()
    {
        var accounts = new Dictionary<Guid, Account>();
        Assert.That(FinancialStatementCategories.Resolve(AccountClass.Expense, null, null, accounts), Is.Null);
        Assert.That(FinancialStatementCategories.Resolve(AccountClass.Equity, null, null, accounts), Is.EqualTo(FinancialStatementCategory.Equity));
    }

    [Test]
    public void IncompatibleAncestorCannotSupplyCategory()
    {
        var parent = Make("1", AccountClass.Asset); parent.SetFinancialStatementCategory(FinancialStatementCategory.CurrentAssets);
        var child = Make("5", parent: parent);
        Assert.That(FinancialStatementCategories.Resolve(child.AccountClass, null, child.ParentAccountId, new[] { parent, child }.ToDictionary(x => x.Id)), Is.Null);
    }
}
