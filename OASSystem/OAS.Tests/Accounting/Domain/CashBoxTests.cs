using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public sealed class CashBoxTests
{
    private static CashAccount Account(string code) => CashAccount.Create(Guid.NewGuid(), code, code, Guid.NewGuid(), Guid.NewGuid(), false, true);

    [Test]
    public void EachNewCashAccountStartsAnOperationalBox()
    {
        var account = Account("001");
        Assert.That(account.CashBoxId, Is.EqualTo(account.Id));
        Assert.That(account.CashBox!.Name, Is.EqualTo("001"));
    }

    [Test]
    public void DifferentCurrencyAccountsCanShareABoxButNotDirectionOrLedger()
    {
        var first = Account("001");
        var second = Account("002");
        second.JoinCashBox(first.CashBoxId);
        first.SetMovementType(CashAccountMovementType.ReceiptOnly);
        second.SetMovementType(CashAccountMovementType.PaymentOnly);
        Assert.That(second.CashBoxId, Is.EqualTo(first.CashBoxId));
        Assert.That(second.Id, Is.Not.EqualTo(first.Id));
        Assert.That(second.AccountId, Is.Not.EqualTo(first.AccountId));
        Assert.That(first.AllowsReceipt && !first.AllowsPayment, Is.True);
        Assert.That(second.AllowsPayment && !second.AllowsReceipt, Is.True);
        Assert.Throws<InvalidOperationException>(() => second.JoinCashBox(Guid.NewGuid()));
    }
}
