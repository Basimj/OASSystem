using NUnit.Framework;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Adjustments;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;
using OAS.Domain.Features.Employees.Leave;
using OAS.Domain.Features.Employees.Loans;
using OAS.Domain.Features.Employees.Overtime;
using OAS.Domain.Features.Employees.Time;

namespace OAS.Tests.Features.Employees.Domain;

[TestFixture]
public sealed class HrOperationsDomainTests
{
    [Test]
    public void WorkShift_OvernightShift_CalculatesScheduledMinutes()
    {
        var shift = WorkShift.Create(
            Guid.NewGuid(),
            "SHF-000001",
            "وردية ليلية",
            null,
            new TimeOnly(20, 0),
            new TimeOnly(4, 0),
            60,
            10,
            5,
            127,
            false,
            true,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(shift.CrossesMidnight, Is.True);
            Assert.That(shift.RawDurationMinutes, Is.EqualTo(480));
            Assert.That(shift.ScheduledMinutes, Is.EqualTo(420));
        });
    }

    [Test]
    public void WorkShift_RejectsBreakEqualToShiftDuration()
    {
        Assert.Throws<DomainException>(() => WorkShift.Create(
            Guid.NewGuid(),
            "SHF-000002",
            "وردية",
            null,
            new TimeOnly(8, 0),
            new TimeOnly(16, 0),
            480,
            0,
            0,
            127,
            false,
            true,
            null));
    }

    [Test]
    public void Attendance_OvernightTimes_CalculateWorkedAndRawOvertime()
    {
        var record = AttendanceRecord.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 10, 2));

        record.ApplySchedule(
            Guid.NewGuid(),
            "SHF-000001",
            "وردية ليلية",
            "Asia/Aden",
            new DateTimeOffset(2026, 10, 2, 17, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 3, 1, 0, 0, TimeSpan.Zero),
            60,
            10,
            5,
            false,
            420,
            AttendanceDayStatus.Absent);

        record.RecordTimes(
            new DateTimeOffset(2026, 10, 2, 17, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 3, 2, 0, 0, TimeSpan.Zero),
            AttendanceSource.Manual,
            0,
            0,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(record.AttendanceStatus, Is.EqualTo(AttendanceDayStatus.Present));
            Assert.That(record.WorkedMinutes, Is.EqualTo(480));
            Assert.That(record.OvertimeMinutes, Is.EqualTo(60));
        });
    }

    [Test]
    public void LeaveBalance_ConsumeAndRestore_PreserveDerivedAvailableBalance()
    {
        var balance = EmployeeLeaveBalance.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            2026,
            opening: 10,
            accrued: 5,
            adjustment: 1);

        balance.Consume(4);
        Assert.That(balance.AvailableDays, Is.EqualTo(12));

        balance.Restore(4);
        Assert.That(balance.AvailableDays, Is.EqualTo(16));
    }

    [Test]
    public void LeaveRequest_OverrideBalance_RequiresReason()
    {
        var leaveType = LeaveType.Create(
            Guid.NewGuid(),
            "LVT-000001",
            "سنوية",
            null,
            true,
            true,
            LeaveAccrualMethod.AnnualGrant,
            LeaveDayCountingMethod.WorkingDays,
            30,
            5,
            true,
            true);

        var request = LeaveRequest.Create(
            Guid.NewGuid(),
            "LVR-2026-000001",
            Guid.NewGuid(),
            leaveType,
            new DateOnly(2026, 10, 4),
            new DateOnly(2026, 10, 5),
            2,
            null);

        request.Submit("admin", DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() =>
            request.Approve(2, true, null, "admin", DateTimeOffset.UtcNow));
    }

    [Test]
    public void ManualOvertime_RequiresReason_AndCannotApproveMoreThanRequested()
    {
        Assert.Throws<DomainException>(() => OvertimeRecord.Create(
            Guid.NewGuid(),
            "OT-2026-000001",
            Guid.NewGuid(),
            null,
            new DateOnly(2026, 10, 2),
            60,
            1.5m,
            null));

        var overtime = OvertimeRecord.Create(
            Guid.NewGuid(),
            "OT-2026-000002",
            Guid.NewGuid(),
            null,
            new DateOnly(2026, 10, 2),
            60,
            1.5m,
            "عمل إضافي معتمد يدويًا");

        overtime.Submit("admin", DateTimeOffset.UtcNow);
        Assert.Throws<DomainException>(() =>
            overtime.Approve(61, "admin", DateTimeOffset.UtcNow));

        overtime.Approve(45, "admin", DateTimeOffset.UtcNow);
        Assert.Multiple(() =>
        {
            Assert.That(overtime.Status, Is.EqualTo(OvertimeStatus.Approved));
            Assert.That(overtime.ApprovedMinutes, Is.EqualTo(45));
        });
    }

    [Test]
    public void LoanInstallment_CannotBePaidTwice()
    {
        var installment = EmployeeLoanInstallment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            new DateOnly(2026, 11, 1),
            10_000m);

        installment.Schedule();
        installment.MarkPaidExternally(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.That(installment.Status, Is.EqualTo(LoanInstallmentStatus.PaidExternally));
        Assert.Throws<DomainException>(() =>
            installment.MarkPaidExternally(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Test]
    public void Adjustment_TypeMustMatchSalaryComponentType()
    {
        var component = SalaryComponent.Create(
            Guid.NewGuid(),
            "SAL-000010",
            "مكافأة",
            null,
            SalaryComponentType.Earning,
            SalaryCalculationMethod.FixedAmount,
            false,
            false,
            false,
            true,
            "OtherPayrollExpense",
            "SalariesPayable",
            10,
            null);

        Assert.Throws<DomainException>(() => EmployeeAdjustment.Create(
            Guid.NewGuid(),
            "ADJ-2026-000001",
            Guid.NewGuid(),
            component,
            EmployeeAdjustmentType.Deduction,
            new DateOnly(2026, 10, 2),
            Guid.NewGuid(),
            "YER",
            "ر.ي",
            2,
            1_000,
            "خصم غير صالح لمكون مكافأة"));
    }
    [Test]
    public void Attendance_PayrollLock_RespectsApprovalPolicy()
    {
        var record = AttendanceRecord.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 10, 2));
        record.ApplySchedule(
            Guid.NewGuid(), "SHF-000010", "صباحية", "Asia/Aden",
            new DateTimeOffset(2026, 10, 2, 5, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 2, 13, 0, 0, TimeSpan.Zero),
            0, 0, 0, false, 480, AttendanceDayStatus.Absent);

        Assert.Throws<DomainException>(() => record.LockToPayroll(Guid.NewGuid(), requireApproved: true));

        var payrollId = Guid.NewGuid();
        Assert.DoesNotThrow(() => record.LockToPayroll(payrollId, requireApproved: false));
        Assert.That(record.EmployeePayrollId, Is.EqualTo(payrollId));
    }

    [Test]
    public void Attendance_RejectedRecord_CannotBeLockedEvenWhenApprovalIsOptional()
    {
        var record = AttendanceRecord.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 10, 2));
        record.ApplySchedule(
            Guid.NewGuid(), "SHF-000011", "صباحية", "Asia/Aden",
            new DateTimeOffset(2026, 10, 2, 5, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 2, 13, 0, 0, TimeSpan.Zero),
            0, 0, 0, false, 480, AttendanceDayStatus.Absent);
        record.Submit("admin", DateTimeOffset.UtcNow);
        record.Reject("invalid", "admin", DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => record.LockToPayroll(Guid.NewGuid(), requireApproved: false));
    }

}
