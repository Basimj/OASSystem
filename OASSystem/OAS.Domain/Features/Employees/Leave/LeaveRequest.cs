using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Leave;

public sealed class LeaveRequest : AuditableEntity<Guid>
{
    private LeaveRequest()
    {
    }
    private LeaveRequest(Guid id, string code, Guid employeeId, Guid leaveTypeId, string typeCode, string typeName, bool isPaid, bool requiresBalance, LeaveDayCountingMethod counting, DateOnly start, DateOnly end, decimal requested, string? reason)
    {
        Id=id;
        LeaveRequestCode=code.Trim();
        EmployeeId=employeeId;
        LeaveTypeId=leaveTypeId;
        LeaveTypeCodeSnapshot=typeCode.Trim();
        LeaveTypeNameSnapshot=typeName.Trim();
        IsPaidSnapshot=isPaid;
        RequiresBalanceSnapshot=requiresBalance;
        DayCountingMethodSnapshot=counting;
        Status=LeaveRequestStatus.Draft;
        UpdateDraft(start, end, requested, reason);
    }
    public string LeaveRequestCode { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public string LeaveTypeCodeSnapshot { get; private set; } = string.Empty;
    public string LeaveTypeNameSnapshot { get; private set; } = string.Empty;
    public bool IsPaidSnapshot { get; private set; }
    public bool RequiresBalanceSnapshot { get; private set; }
    public LeaveDayCountingMethod DayCountingMethodSnapshot { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public decimal RequestedDays { get; private set; }
    public decimal? ApprovedDays { get; private set; }
    public LeaveRequestStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public bool BalanceOverrideUsed { get; private set; }
    public string? BalanceOverrideReason { get; private set; }
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
    public byte[] RowVersion { get; private set; } = [];
    public static LeaveRequest Create(Guid id, string code, Guid emp, LeaveType type, DateOnly start, DateOnly end, decimal requested, string? reason)
    {
        if (id==Guid.Empty||emp==Guid.Empty||type.Id==Guid.Empty||string.IsNullOrWhiteSpace(code))throw new DomainException("Leave request identity is required.");
        return new(id, code, emp, type.Id, type.LeaveTypeCode, type.NameAr, type.IsPaid, type.RequiresBalance, type.DayCountingMethod, start, end, requested, reason);
    }
    public void UpdateDraft(DateOnly start, DateOnly end, decimal requested, string? reason)
    {
        if (Status!=LeaveRequestStatus.Draft)throw new DomainException("Only draft leave requests can be edited.");
        if (end<start||requested<=0)throw new DomainException("Leave request dates or days are invalid.");
        StartDate=start;
        EndDate=end;
        RequestedDays=requested;
        Reason=N(reason);
        if (Reason is
        {
            Length:>500
        }
        )throw new DomainException("Leave reason cannot exceed 500 characters.");
    }
    public void Submit(string? actor, DateTimeOffset at)
    {
        if (Status!=LeaveRequestStatus.Draft)throw new DomainException("Only draft leave requests can be submitted.");
        Status=LeaveRequestStatus.PendingApproval;
        SubmittedBy=N(actor);
        SubmittedAtUtc=at;
    }
    public void Approve(decimal approvedDays, bool overrideBalance, string? overrideReason, string? actor, DateTimeOffset at)
    {
        if (Status!=LeaveRequestStatus.PendingApproval)throw new DomainException("Only pending leave requests can be approved.");
        if (approvedDays<=0||approvedDays>RequestedDays)throw new DomainException("Approved leave days are invalid.");
        if (overrideBalance&&string.IsNullOrWhiteSpace(overrideReason))throw new DomainException("Balance override reason is required.");
        ApprovedDays=approvedDays;
        BalanceOverrideUsed=overrideBalance;
        BalanceOverrideReason=N(overrideReason);
        ApprovedBy=N(actor);
        ApprovedAtUtc=at;
        Status=LeaveRequestStatus.Approved;
    }
    public void Reject(string reason, string? actor, DateTimeOffset at)
    {
        if (Status!=LeaveRequestStatus.PendingApproval)throw new DomainException("Only pending leave requests can be rejected.");
        if (string.IsNullOrWhiteSpace(reason))throw new DomainException("Rejection reason is required.");
        RejectedBy=N(actor);
        RejectedAtUtc=at;
        RejectionReason=reason.Trim();
        Status=LeaveRequestStatus.Rejected;
    }
    public void Cancel(string reason, string? actor, DateTimeOffset at)
    {
        if (Status is LeaveRequestStatus.Rejected or LeaveRequestStatus.Cancelled)throw new DomainException("Leave request cannot be cancelled from current status.");
        if (string.IsNullOrWhiteSpace(reason))throw new DomainException("Cancellation reason is required.");
        CancelledBy=N(actor);
        CancelledAtUtc=at;
        CancellationReason=reason.Trim();
        Status=LeaveRequestStatus.Cancelled;
    }
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
}
