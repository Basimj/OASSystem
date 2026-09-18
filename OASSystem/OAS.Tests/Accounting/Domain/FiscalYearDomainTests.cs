using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public class FiscalYearDomainTests
{
    private Guid _yearId;
    private Guid _periodId;
    private Guid _userId;

    [SetUp]
    public void Setup()
    {
        _yearId = Guid.NewGuid();
        _periodId = Guid.NewGuid();
        _userId = Guid.NewGuid();
    }

    [Test]
    public void Create_ValidFiscalYear_InitializesProperties()
    {
        var year = FiscalYear.Create(
            _yearId,
            "FY2026",
            "السنة المالية 2026",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            FiscalYearStatus.Open);

        Assert.That(year.Id, Is.EqualTo(_yearId));
        Assert.That(year.Code, Is.EqualTo("FY2026"));
        Assert.That(year.Status, Is.EqualTo(FiscalYearStatus.Open));
        Assert.That(year.StartDate, Is.EqualTo(new DateOnly(2026, 1, 1)));
        Assert.That(year.EndDate, Is.EqualTo(new DateOnly(2026, 12, 31)));
    }

    [Test]
    public void Create_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            FiscalYear.Create(
                _yearId,
                "FY2026",
                "السنة المالية 2026",
                new DateOnly(2026, 12, 31),
                new DateOnly(2026, 1, 1),
                FiscalYearStatus.Open));
    }

    [Test]
    public void FiscalYear_ClosingWorkflow_TransitionsStatusCorrectly()
    {
        var year = FiscalYear.Create(
            _yearId, "FY2026", "2026",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            FiscalYearStatus.Open);

        year.StartClosing();
        Assert.That(year.Status, Is.EqualTo(FiscalYearStatus.Closing));

        var closedAt = DateTime.UtcNow;
        year.Close(_userId, closedAt);
        Assert.That(year.Status, Is.EqualTo(FiscalYearStatus.Closed));
        Assert.That(year.ClosedBy, Is.EqualTo(_userId));
        Assert.That(year.ClosedAtUtc, Is.EqualTo(closedAt));
    }

    [Test]
    public void FiscalPeriod_CreateAndSetLocks_WorksCorrectly()
    {
        var period = FiscalPeriod.Create(
            _periodId,
            _yearId,
            1,
            "يناير 2026",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            FiscalPeriodStatus.Open,
            salesLocked: false,
            inventoryLocked: false,
            accountingLocked: false);

        Assert.That(period.PeriodNumber, Is.EqualTo(1));
        Assert.That(period.SalesLocked, Is.False);
        Assert.That(period.InventoryLocked, Is.False);
        Assert.That(period.AccountingLocked, Is.False);

        period.SetLocks(salesLocked: true, inventoryLocked: true, accountingLocked: true);
        Assert.That(period.SalesLocked, Is.True);
        Assert.That(period.InventoryLocked, Is.True);
        Assert.That(period.AccountingLocked, Is.True);
        Assert.That(period.CanPostAccounting(), Is.False);
    }
}
