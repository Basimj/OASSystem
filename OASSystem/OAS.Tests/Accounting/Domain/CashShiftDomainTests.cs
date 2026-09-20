using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public class CashShiftDomainTests
{
    private Guid _shiftId;
    private Guid _cashAccountId;
    private Guid _userId;

    [SetUp]
    public void Setup()
    {
        _shiftId = Guid.NewGuid();
        _cashAccountId = Guid.NewGuid();
        _userId = Guid.NewGuid();
    }

    [Test]
    public void Create_ValidParameters_CreatesOpenShift()
    {
        var shift = CashShift.Create(
            _shiftId,
            "CS-20260101-001",
            _cashAccountId,
            _userId,
            DateTime.UtcNow,
            openingBalance: 500m,
            status: CashShiftStatus.Open);

        Assert.That(shift.Id, Is.EqualTo(_shiftId));
        Assert.That(shift.ShiftNumber, Is.EqualTo("CS-20260101-001"));
        Assert.That(shift.OpeningBalance, Is.EqualTo(500m));
        Assert.That(shift.Status, Is.EqualTo(CashShiftStatus.Open));
        Assert.That(shift.DifferenceAmount, Is.Null);
    }

    [Test]
    public void Create_NegativeOpeningBalance_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CashShift.Create(
                _shiftId,
                "CS-20260101-001",
                _cashAccountId,
                _userId,
                DateTime.UtcNow,
                openingBalance: -10m,
                status: CashShiftStatus.Open));
    }

    [Test]
    public void StartClosing_OpenShift_SetsStatusToClosing()
    {
        var shift = CashShift.Create(
            _shiftId, "CS-20260101-001", _cashAccountId, _userId,
            DateTime.UtcNow, 500m, CashShiftStatus.Open);

        shift.StartClosing(expectedClosingBalance: 1200m);

        Assert.That(shift.Status, Is.EqualTo(CashShiftStatus.Closing));
        Assert.That(shift.ExpectedClosingBalance, Is.EqualTo(1200m));
    }

    [Test]
    public void Close_ClosingShift_CalculatesDifferenceAndSetsClosed()
    {
        var shift = CashShift.Create(
            _shiftId, "CS-20260101-001", _cashAccountId, _userId,
            DateTime.UtcNow, 500m, CashShiftStatus.Open);

        shift.StartClosing(expectedClosingBalance: 1000m);

        var closedAt = DateTime.UtcNow;
        shift.Close(actualClosingBalance: 980m, closedBy: _userId, closedAtUtc: closedAt);

        Assert.That(shift.Status, Is.EqualTo(CashShiftStatus.Closed));
        Assert.That(shift.ActualClosingBalance, Is.EqualTo(980m));
        Assert.That(shift.DifferenceAmount, Is.EqualTo(-20m)); // Shortage of 20
        Assert.That(shift.ClosedBy, Is.EqualTo(_userId));
        Assert.That(shift.ClosedAtUtc, Is.EqualTo(closedAt));
    }

    [Test]
    public void Close_DirectlyFromOpen_ThrowsInvalidOperationException()
    {
        var shift = CashShift.Create(
            _shiftId, "CS-20260101-001", _cashAccountId, _userId,
            DateTime.UtcNow, 500m, CashShiftStatus.Open);

        Assert.Throws<InvalidOperationException>(() =>
            shift.Close(1000m, _userId, DateTime.UtcNow));
    }
}
