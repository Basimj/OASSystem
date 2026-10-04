using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.EndOfService;
using OAS.Domain.Features.Employees.Enums;
using OAS.Domain.Features.Employees.Payroll;

namespace OAS.Tests.Features.Employees.Domain;

[TestFixture]
public sealed class PayrollDomainTests
{
    [Test]
    public void PayrollPeriod_UsesDeterministicPeriodCode_AndLifecycle()
    {
        var period = PayrollPeriod.Create(Guid.NewGuid(), 2026, 10, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        Assert.That(period.PeriodCode, Is.EqualTo("PAY-2026-10"));
        Assert.That(period.Status, Is.EqualTo(PayrollPeriodStatus.Open));

        period.Lock("admin", DateTimeOffset.UtcNow);
        Assert.That(period.Status, Is.EqualTo(PayrollPeriodStatus.Locked));
        period.Reopen();
        Assert.That(period.Status, Is.EqualTo(PayrollPeriodStatus.Open));
        period.Lock("admin", DateTimeOffset.UtcNow);
        period.Close("admin", DateTimeOffset.UtcNow);
        Assert.That(period.Status, Is.EqualTo(PayrollPeriodStatus.Closed));
    }

    [Test]
    public void PayrollPolicy_IsImmutableAfterActivation()
    {
        var policy = PayrollPolicy.Create(
            Guid.NewGuid(), "POL-000001", "سياسة", new DateOnly(2026, 1, 1), null,
            PayrollProrationMethod.CalendarDays,
            PayrollDailyRateMethod.Fixed30Days,
            PayrollHourlyRateMethod.DailyRateByContractHours,
            true, false, null, null, null, null, null, null);

        policy.Activate("admin", DateTimeOffset.UtcNow);

        Assert.That(policy.Status, Is.EqualTo(PayrollPolicyStatus.Active));
        Assert.Throws<DomainException>(() => policy.UpdateDraft(
            "معدلة", new DateOnly(2026, 1, 1), null,
            PayrollProrationMethod.None,
            PayrollDailyRateMethod.Fixed30Days,
            PayrollHourlyRateMethod.DailyRateByContractHours,
            true, false, null, null, null, null, null, null));
    }

    [Test]
    public void EmployeePayroll_SeparatesEmployerContributionFromNetPay()
    {
        var payroll = EmployeePayroll.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31),
            "EMP-0001", "أحمد", "مندوب", "المبيعات", null, null,
            Guid.NewGuid(), "YER", "ر.ي", 2,
            configuredBasic: 100_000m,
            calculatedBasic: 100_000m,
            gross: 120_000m,
            deductions: 10_000m,
            employer: 7_500m,
            net: 110_000m);

        Assert.Multiple(() =>
        {
            Assert.That(payroll.GrossEarnings, Is.EqualTo(120_000m));
            Assert.That(payroll.TotalEmployerContributions, Is.EqualTo(7_500m));
            Assert.That(payroll.NetPay, Is.EqualTo(110_000m));
        });
    }

    [Test]
    public void EmployeePayroll_RejectsNegativeNetOrUnbalancedTotals()
    {
        Assert.Throws<DomainException>(() => EmployeePayroll.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31),
            "EMP-0001", "أحمد", null, null, null, null,
            Guid.NewGuid(), "YER", null, 2,
            100m, 100m, 100m, 30m, 0m, 80m));
    }

    [Test]
    public void PayrollRun_CannotPostBeforeApproval()
    {
        var run = PayrollRun.Create(
            Guid.NewGuid(), "PRUN-2026-000001", Guid.NewGuid(), PayrollRunType.Regular,
            new DateOnly(2026, 10, 31), new DateOnly(2026, 10, 31), Guid.NewGuid(),
            Guid.NewGuid(), "YER", null, 2);

        Assert.Throws<DomainException>(() => run.MarkPosted(Guid.NewGuid(), "admin", DateTimeOffset.UtcNow));
    }

    [Test]
    public void PaymentAllocation_PreservesSeparateSourceAndTargetBaseAmounts()
    {
        var allocation = PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(), null, Guid.NewGuid(), AllocationTargetDocumentType.EmployeePayroll, Guid.NewGuid(),
            Guid.NewGuid(), "USD", 100m, 260m, 26_000m, DateTime.UtcNow, 25_000m);

        Assert.Multiple(() =>
        {
            Assert.That(allocation.BaseAllocatedAmount, Is.EqualTo(26_000m));
            Assert.That(allocation.TargetBaseAllocatedAmount, Is.EqualTo(25_000m));
        });
    }

    [Test]
    public void PaymentVoucherLine_StoresRealizedFxDifference()
    {
        var line = PaymentVoucherLine.CreateSettlement(
            Guid.NewGuid(), Guid.NewGuid(), 1, SettlementPartyType.Employee,
            null, null, Guid.NewGuid(), "أحمد", Guid.NewGuid(), PaymentMethod.BankTransfer,
            null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "USD", "$", 2,
            1_000m, 260m, new DateOnly(2026, 10, 31), ExchangeRateType.Accounting, ExchangeRateSource.System,
            260_000m, "PAY", new DateOnly(2026, 10, 31), "EmployeePayroll", Guid.NewGuid(), "راتب",
            counterpartyBaseAmount: 250_000m,
            realizedExchangeDifferenceBase: 10_000m);

        Assert.Multiple(() =>
        {
            Assert.That(line.CounterpartyBaseAmount, Is.EqualTo(250_000m));
            Assert.That(line.RealizedExchangeDifferenceBase, Is.EqualTo(10_000m));
        });
    }

    [Test]
    public void EndOfService_ZeroNetCanBecomePaidAfterPosting()
    {
        var settlement = EndOfServiceSettlement.Create(
            Guid.NewGuid(), "EOS-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 10, 15), Guid.NewGuid(), "YER", null, 2, null);

        settlement.SetCalculated(0, 0, 0, 0, 0, 0, "admin", DateTimeOffset.UtcNow);
        settlement.Review("admin", DateTimeOffset.UtcNow);
        settlement.Approve("admin", DateTimeOffset.UtcNow);
        settlement.SetPostingSnapshot(1m, new DateOnly(2026, 10, 15), 1, 1, 0m, 0m);
        settlement.MarkPosted(Guid.NewGuid(), "admin", DateTimeOffset.UtcNow);
        settlement.MarkPaid();

        Assert.That(settlement.Status, Is.EqualTo(EndOfServiceStatus.Paid));
    }
    [Test]
    public void PayrollRun_RecalculationRequiresReopenAfterReview()
    {
        var run = PayrollRun.Create(
            Guid.NewGuid(), "PRUN-2026-000002", Guid.NewGuid(), PayrollRunType.Regular,
            new DateOnly(2026, 10, 31), new DateOnly(2026, 10, 31), Guid.NewGuid(),
            Guid.NewGuid(), "YER", null, 2);

        run.MarkCalculated(100m, 10m, 0m, 90m, "admin", DateTimeOffset.UtcNow);
        run.Review("admin", DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() =>
            run.MarkCalculated(100m, 10m, 0m, 90m, "admin", DateTimeOffset.UtcNow));

        run.Reopen();
        Assert.DoesNotThrow(() =>
            run.MarkCalculated(100m, 10m, 0m, 90m, "admin", DateTimeOffset.UtcNow));
    }

    [Test]
    public void AllocationTargetDocumentType_PreservesLegacyValues_AndAppendsHrTargets()
    {
        Assert.Multiple(() =>
        {
            Assert.That((byte)AllocationTargetDocumentType.SalesInvoice, Is.EqualTo(1));
            Assert.That((byte)AllocationTargetDocumentType.PurchaseInvoice, Is.EqualTo(2));
            Assert.That((byte)AllocationTargetDocumentType.DebitAdjustment, Is.EqualTo(3));
            Assert.That((byte)AllocationTargetDocumentType.CreditAdjustment, Is.EqualTo(4));
            Assert.That((byte)AllocationTargetDocumentType.EmployeePayroll, Is.EqualTo(5));
            Assert.That((byte)AllocationTargetDocumentType.EndOfServiceSettlement, Is.EqualTo(6));
        });
    }

}
