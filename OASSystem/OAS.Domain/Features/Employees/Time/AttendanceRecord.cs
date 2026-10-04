using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Time;

public sealed class AttendanceRecord : AuditableEntity<Guid>
{
    private AttendanceRecord()
    {
    }
    private AttendanceRecord(Guid id, Guid employeeId, DateOnly date)
    {
        Id=id;
        EmployeeId=employeeId;
        AttendanceDate=date;
        ApprovalStatus=AttendanceApprovalStatus.Draft;
        Source=AttendanceSource.System;
    }
    public Guid EmployeeId { get; private set; }
    public DateOnly AttendanceDate { get; private set; }
    public Guid? WorkShiftId { get; private set; }
    public string? ShiftCodeSnapshot { get; private set; }
    public string? ShiftNameSnapshot { get; private set; }
    public string? TimeZoneIdSnapshot { get; private set; }
    public DateTimeOffset? ScheduledStartAtUtc { get; private set; }
    public DateTimeOffset? ScheduledEndAtUtc { get; private set; }
    public int BreakMinutesSnapshot { get; private set; }
    public int GraceLateMinutesSnapshot { get; private set; }
    public int GraceEarlyLeaveMinutesSnapshot { get; private set; }
    public bool IsFlexibleSnapshot { get; private set; }
    public DateTimeOffset? CheckInAtUtc { get; private set; }
    public DateTimeOffset? CheckOutAtUtc { get; private set; }
    public AttendanceSource Source { get; private set; }
    public AttendanceDayStatus AttendanceStatus { get; private set; }
    public AttendanceApprovalStatus ApprovalStatus { get; private set; }
    public int ScheduledMinutes { get; private set; }
    public int WorkedMinutes { get; private set; }
    public int LateMinutes { get; private set; }
    public int EarlyLeaveMinutes { get; private set; }
    public int OvertimeMinutes { get; private set; }
    public Guid? SourceLeaveRequestId { get; private set; }
    public Guid? SourceHolidayId { get; private set; }
    public string? SubmittedBy { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public string? RejectedBy { get; private set; }
    public DateTimeOffset? RejectedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? LastCorrectedBy { get; private set; }
    public DateTimeOffset? LastCorrectedAtUtc { get; private set; }
    public string? LastCorrectionReason { get; private set; }
    public string? Notes { get; private set; }
    public Guid? EmployeePayrollId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static AttendanceRecord Create(Guid id, Guid employeeId, DateOnly date)
    {
        if (id==Guid.Empty||employeeId==Guid.Empty)throw new DomainException("Attendance identity is required.");
        return new(id, employeeId, date);
    }
    public void ApplySchedule(Guid? shiftId, string? shiftCode, string? shiftName, string? timeZoneId, DateTimeOffset? scheduledStartUtc, DateTimeOffset? scheduledEndUtc, int breakMinutes, int graceLateMinutes, int graceEarlyLeaveMinutes, bool isFlexible, int scheduledMinutes, AttendanceDayStatus status, Guid? leaveId=null, Guid? holidayId=null)
    {
        EnsureEditable();
        if (breakMinutes<0||graceLateMinutes<0||graceEarlyLeaveMinutes<0||scheduledMinutes<0)throw new DomainException("Attendance schedule values cannot be negative.");
        WorkShiftId=shiftId;
        ShiftCodeSnapshot=N(shiftCode);
        ShiftNameSnapshot=N(shiftName);
        TimeZoneIdSnapshot=N(timeZoneId);
        ScheduledStartAtUtc=scheduledStartUtc;
        ScheduledEndAtUtc=scheduledEndUtc;
        BreakMinutesSnapshot=breakMinutes;
        GraceLateMinutesSnapshot=graceLateMinutes;
        GraceEarlyLeaveMinutesSnapshot=graceEarlyLeaveMinutes;
        IsFlexibleSnapshot=isFlexible;
        ScheduledMinutes=scheduledMinutes;
        AttendanceStatus=status;
        SourceLeaveRequestId=leaveId;
        SourceHolidayId=holidayId;
        Recalculate();
    }
    public void RecordTimes(DateTimeOffset? checkIn, DateTimeOffset? checkOut, AttendanceSource source, int lateMinutes, int earlyLeaveMinutes, string? notes, string? correctionReason=null, string? actor=null, DateTimeOffset? correctedAt=null)
    {
        EnsureEditable();
        if (checkIn.HasValue&&checkOut.HasValue&&checkOut.Value<checkIn.Value)throw new DomainException("Check-out cannot be before check-in.");
        if (lateMinutes<0||earlyLeaveMinutes<0)throw new DomainException("Attendance minute values cannot be negative.");
        CheckInAtUtc=checkIn;
        CheckOutAtUtc=checkOut;
        Source=source;
        LateMinutes=lateMinutes;
        EarlyLeaveMinutes=earlyLeaveMinutes;
        Notes=N(notes);
        if (!string.IsNullOrWhiteSpace(correctionReason))
        {
            LastCorrectionReason=N(correctionReason);
            LastCorrectedBy=N(actor);
            LastCorrectedAtUtc=correctedAt;
        }
        Recalculate();
    }
    public void MarkStatus(AttendanceDayStatus status, Guid? leaveId=null, Guid? holidayId=null)
    {
        EnsureEditable();
        AttendanceStatus=status;
        SourceLeaveRequestId=leaveId;
        SourceHolidayId=holidayId;
        Recalculate();
    }
    public void Submit(string? actor, DateTimeOffset at)
    {
        if (ApprovalStatus!=AttendanceApprovalStatus.Draft&&ApprovalStatus!=AttendanceApprovalStatus.Rejected)throw new DomainException("Only draft or rejected attendance can be submitted.");
        ApprovalStatus=AttendanceApprovalStatus.PendingApproval;
        SubmittedBy=N(actor);
        SubmittedAtUtc=at;
        RejectedBy=null;
        RejectedAtUtc=null;
        RejectionReason=null;
    }
    public void Approve(string? actor, DateTimeOffset at)
    {
        if (ApprovalStatus!=AttendanceApprovalStatus.PendingApproval)throw new DomainException("Only pending attendance can be approved.");
        ApprovalStatus=AttendanceApprovalStatus.Approved;
        ApprovedBy=N(actor);
        ApprovedAtUtc=at;
    }
    public void Reject(string reason, string? actor, DateTimeOffset at)
    {
        if (ApprovalStatus!=AttendanceApprovalStatus.PendingApproval)throw new DomainException("Only pending attendance can be rejected.");
        if (string.IsNullOrWhiteSpace(reason))throw new DomainException("Rejection reason is required.");
        ApprovalStatus=AttendanceApprovalStatus.Rejected;
        RejectedBy=N(actor);
        RejectedAtUtc=at;
        RejectionReason=reason.Trim();
    }
    public void ReopenForCorrection(string reason, string? actor, DateTimeOffset at)
    {
        if (ApprovalStatus!=AttendanceApprovalStatus.Approved)throw new DomainException("Only approved attendance can be reopened.");
        if (string.IsNullOrWhiteSpace(reason))throw new DomainException("Correction reason is required.");
        ApprovalStatus=AttendanceApprovalStatus.Draft;
        LastCorrectionReason=reason.Trim();
        LastCorrectedBy=N(actor);
        LastCorrectedAtUtc=at;
        ApprovedBy=null;
        ApprovedAtUtc=null;
    }
    public void LockToPayroll(Guid payrollId, bool requireApproved)
    {
        if (payrollId == Guid.Empty) throw new DomainException("Payroll is required when locking attendance.");
        if (requireApproved && ApprovalStatus != AttendanceApprovalStatus.Approved) throw new DomainException("Only approved attendance can be locked when payroll policy requires approval.");
        if (!requireApproved && ApprovalStatus == AttendanceApprovalStatus.Rejected) throw new DomainException("Rejected attendance cannot be locked to payroll.");
        if (AttendanceStatus == AttendanceDayStatus.Incomplete) throw new DomainException("Incomplete attendance cannot be locked to payroll.");
        if (EmployeePayrollId.HasValue && EmployeePayrollId.Value != payrollId) throw new DomainException("Attendance is already used by another payroll.");
        EmployeePayrollId = payrollId;
    }
    private void Recalculate()
    {
        if (CheckInAtUtc.HasValue&&CheckOutAtUtc.HasValue)
        {
            var gross=(int)Math.Max(0, (CheckOutAtUtc.Value-CheckInAtUtc.Value).TotalMinutes);
            WorkedMinutes=Math.Max(0, gross-BreakMinutesSnapshot);
            OvertimeMinutes=Math.Max(0, WorkedMinutes-ScheduledMinutes);
            if (AttendanceStatus is not(AttendanceDayStatus.Leave or AttendanceDayStatus.Holiday or AttendanceDayStatus.Weekend))AttendanceStatus=AttendanceDayStatus.Present;
        }
        else
        {
            WorkedMinutes=0;
            OvertimeMinutes=0;
            if (CheckInAtUtc.HasValue||CheckOutAtUtc.HasValue)AttendanceStatus=AttendanceDayStatus.Incomplete;
        }
    }
    private void EnsureEditable()
    {
        if (EmployeePayrollId.HasValue) throw new DomainException("Attendance used by posted payroll cannot be edited.");
        if (ApprovalStatus==AttendanceApprovalStatus.Approved)throw new DomainException("Approved attendance must be reopened before editing.");
    }
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
}
