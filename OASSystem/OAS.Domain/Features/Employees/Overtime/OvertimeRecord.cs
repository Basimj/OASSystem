using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Overtime;

public sealed class OvertimeRecord : AuditableEntity<Guid>
{
    private OvertimeRecord()
    {
    }
    private OvertimeRecord(Guid id, string code, Guid emp, Guid? attendance, DateOnly date, int requested, decimal multiplier, string? reason)
    {
        Id=id;
        OvertimeCode=code.Trim();
        EmployeeId=emp;
        AttendanceRecordId=attendance;
        WorkDate=date;
        Status=OvertimeStatus.Draft;
        UpdateDraft(requested, multiplier, reason);
    }
    public string OvertimeCode { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid? AttendanceRecordId { get; private set; }
    public DateOnly WorkDate { get; private set; }
    public int RequestedMinutes { get; private set; }
    public int ApprovedMinutes { get; private set; }
    public decimal RateMultiplier { get; private set; }
    public OvertimeStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public string? SubmittedBy { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public string? RejectedBy { get; private set; }
    public DateTimeOffset? RejectedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? CancelledBy { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public Guid? EmployeePayrollId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static OvertimeRecord Create(Guid id, string code, Guid emp, Guid? attendance, DateOnly date, int requested, decimal multiplier, string? reason)
    {
        if (id==Guid.Empty||emp==Guid.Empty||string.IsNullOrWhiteSpace(code))throw new DomainException("Overtime identity is required.");
        return new(id, code, emp, attendance, date, requested, multiplier, reason);
    }
    public void UpdateDraft(int requested, decimal multiplier, string? reason)
    {
        if (Status!=OvertimeStatus.Draft)throw new DomainException("Only draft overtime can be edited.");
        if (requested<=0||multiplier<=0)throw new DomainException("Overtime minutes and multiplier must be positive.");
        RequestedMinutes=requested;
        RateMultiplier=multiplier;
        Reason=N(reason);
        if (AttendanceRecordId is null&&string.IsNullOrWhiteSpace(Reason))throw new DomainException("Reason is required for manual overtime.");
    }
    public void Submit(string? actor, DateTimeOffset at)
    {
        if (Status!=OvertimeStatus.Draft)throw new DomainException("Only draft overtime can be submitted.");
        Status=OvertimeStatus.PendingApproval;
        SubmittedBy=N(actor);
        SubmittedAtUtc=at;
    }
    public void Approve(int minutes, string? actor, DateTimeOffset at)
    {
        if (Status!=OvertimeStatus.PendingApproval)throw new DomainException("Only pending overtime can be approved.");
        if (minutes<0||minutes>RequestedMinutes)throw new DomainException("Approved overtime minutes are invalid.");
        ApprovedMinutes=minutes;
        ApprovedBy=N(actor);
        ApprovedAtUtc=at;
        Status=OvertimeStatus.Approved;
    }
    public void Reject(string reason, string? actor, DateTimeOffset at)
    {
        if (Status!=OvertimeStatus.PendingApproval||string.IsNullOrWhiteSpace(reason))throw new DomainException("Overtime rejection is invalid.");
        RejectedBy=N(actor);
        RejectedAtUtc=at;
        RejectionReason=reason.Trim();
        Status=OvertimeStatus.Rejected;
    }
    public void Cancel(string reason, string? actor, DateTimeOffset at)
    {
        if (Status==OvertimeStatus.AppliedToPayroll)throw new DomainException("Applied overtime cannot be cancelled.");
        if (string.IsNullOrWhiteSpace(reason))throw new DomainException("Cancellation reason is required.");
        CancelledBy=N(actor);
        CancelledAtUtc=at;
        CancellationReason=reason.Trim();
        Status=OvertimeStatus.Cancelled;
    }
    public void MarkAppliedToPayroll(Guid payrollId)
    {
        if (Status!=OvertimeStatus.Approved||payrollId==Guid.Empty)throw new DomainException("Only approved overtime can be applied to payroll.");
        EmployeePayrollId=payrollId;
        Status=OvertimeStatus.AppliedToPayroll;
    }
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
}
