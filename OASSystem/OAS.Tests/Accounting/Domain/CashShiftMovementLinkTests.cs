using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public sealed class CashShiftMovementLinkTests
{
    [Test]
    public void NewShiftUsesLinkedMovementsAndCannotReassignLine()
    {
        var shift = CashShift.Create(Guid.NewGuid(), "CS-TEST", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, 100m, CashShiftStatus.Open);
        var line = JournalEntryLine.Create(Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), 50m, 0m, null, null, null, null, null, null);
        Assert.That(shift.UsesLinkedMovements, Is.True);
        line.AssignCashShift(shift.Id);
        line.AssignCashShift(shift.Id);
        Assert.That(line.CashShiftId, Is.EqualTo(shift.Id));
        Assert.Throws<InvalidOperationException>(() => line.AssignCashShift(Guid.NewGuid()));
        Assert.That(line.CreateOpposite(Guid.NewGuid(), Guid.NewGuid()).CashShiftId, Is.Null);
    }

    [Test]
    public void ReconcileClosingBalancePreservesActualCount()
    {
        var now = DateTime.UtcNow;
        var shift = CashShift.Create(Guid.NewGuid(), "CS-TEST", Guid.NewGuid(), Guid.NewGuid(), now, 100000m, CashShiftStatus.Open);
        shift.StartClosing(100000m);
        shift.Close(125000m, Guid.NewGuid(), now.AddMinutes(1));
        shift.ReconcileClosingBalance(130000m);
        Assert.That(shift.ActualClosingBalance, Is.EqualTo(125000m));
        Assert.That(shift.DifferenceAmount, Is.EqualTo(-5000m));
        shift.Approve();
        Assert.Throws<InvalidOperationException>(() => shift.ReconcileClosingBalance(125000m));
    }
}
