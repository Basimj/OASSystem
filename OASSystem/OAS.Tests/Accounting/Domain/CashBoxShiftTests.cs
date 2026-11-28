using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
namespace OAS.Tests.Accounting.Domain;
[TestFixture]
public sealed class CashBoxShiftTests
{
    [Test]
    public void GroupedShiftKeepsIndependentCurrencyAccountAndCount()
    {
        var group=Guid.NewGuid(); var a=Guid.NewGuid(); var b=Guid.NewGuid(); var actor=Guid.NewGuid();
        var first=CashShift.Create(Guid.NewGuid(),"CS-1",a,actor,DateTime.UtcNow,100,CashShiftStatus.Open);
        var second=CashShift.Create(Guid.NewGuid(),"CS-2",b,actor,DateTime.UtcNow,20,CashShiftStatus.Open);
        Assert.That(first.CashBoxShiftId,Is.Null);
        first.JoinCashBoxShift(group); second.JoinCashBoxShift(group);
        Assert.That(first.CashBoxShiftId,Is.EqualTo(second.CashBoxShiftId));
        Assert.That(first.CashAccountId,Is.Not.EqualTo(second.CashAccountId));
        Assert.That(first.OpeningBalance,Is.EqualTo(100)); Assert.That(second.OpeningBalance,Is.EqualTo(20));
        Assert.Throws<InvalidOperationException>(()=>first.JoinCashBoxShift(Guid.NewGuid()));
    }
}
