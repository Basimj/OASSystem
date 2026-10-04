using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Adjustments;

public sealed class EmployeeAdjustment : AuditableEntity<Guid>
{
    private EmployeeAdjustment()
    {
    }
    private EmployeeAdjustment(Guid id, string code, Guid emp, Guid componentId, string compCode, string compName, SalaryComponentType compType, string? debit, string? credit, EmployeeAdjustmentType type, DateOnly effective, Guid currencyId, string currCode, string? symbol, byte decimals, decimal amount, string reason)
    {
        Id=id;
        AdjustmentCode=code.Trim();
        EmployeeId=emp;
        SalaryComponentId=componentId;
        ComponentCodeSnapshot=compCode.Trim();
        ComponentNameSnapshot=compName.Trim();
        ComponentTypeSnapshot=compType;
        DebitPostingRoleSnapshot=N(debit);
        CreditPostingRoleSnapshot=N(credit);
        EffectiveDate=effective;
        CurrencyId=currencyId;
        CurrencyCodeSnapshot=currCode.Trim().ToUpperInvariant();
        CurrencySymbolSnapshot=N(symbol);
        CurrencyDecimalPlacesSnapshot=decimals;
        Status=EmployeeAdjustmentStatus.Draft;
        UpdateDraft(type, amount, reason);
    }
    public string AdjustmentCode { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid SalaryComponentId { get; private set; }
    public string ComponentCodeSnapshot { get; private set; } = string.Empty;
    public string ComponentNameSnapshot { get; private set; } = string.Empty;
    public SalaryComponentType ComponentTypeSnapshot { get; private set; }
    public string? DebitPostingRoleSnapshot { get; private set; }
    public string? CreditPostingRoleSnapshot { get; private set; }
    public EmployeeAdjustmentType AdjustmentType { get; private set; }
    public DateOnly EffectiveDate { get; private set; }
    public Guid CurrencyId { get; private set; }
    public string CurrencyCodeSnapshot { get; private set; } = string.Empty;
    public string? CurrencySymbolSnapshot { get; private set; }
    public byte CurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal Amount { get; private set; }
    public EmployeeAdjustmentStatus Status { get; private set; }
    public string Reason { get; private set; } = string.Empty;
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
    public static EmployeeAdjustment Create(Guid id, string code, Guid emp, SalaryComponent comp, EmployeeAdjustmentType type, DateOnly effective, Guid currencyId, string currCode, string? symbol, byte decimals, decimal amount, string reason)
    {
        if (id==Guid.Empty||emp==Guid.Empty||comp.Id==Guid.Empty||currencyId==Guid.Empty||string.IsNullOrWhiteSpace(code)||string.IsNullOrWhiteSpace(currCode))throw new DomainException("Adjustment identity is invalid.");
        return new(id, code, emp, comp.Id, comp.ComponentCode, comp.NameAr, comp.ComponentType, comp.DebitPostingRole, comp.CreditPostingRole, type, effective, currencyId, currCode, symbol, decimals, amount, reason);
    }
    public void UpdateDraft(EmployeeAdjustmentType type, decimal amount, string reason)
    {
        if (Status!=EmployeeAdjustmentStatus.Draft)throw new DomainException("Only draft adjustments can be edited.");
        if (amount<=0)throw new DomainException("Adjustment amount must be positive.");
        if (string.IsNullOrWhiteSpace(reason))throw new DomainException("Adjustment reason is required.");
        if (type==EmployeeAdjustmentType.Earning&&ComponentTypeSnapshot!=SalaryComponentType.Earning||type==EmployeeAdjustmentType.Deduction&&ComponentTypeSnapshot!=SalaryComponentType.Deduction)throw new DomainException("Adjustment type does not match salary component type.");
        AdjustmentType=type;
        Amount=amount;
        Reason=reason.Trim();
        if (Reason.Length>500)throw new DomainException("Adjustment reason cannot exceed 500 characters.");
    }
    public void Submit(string? actor, DateTimeOffset at)
    {
        if (Status!=EmployeeAdjustmentStatus.Draft)throw new DomainException("Only draft adjustments can be submitted.");
        Status=EmployeeAdjustmentStatus.PendingApproval;
        SubmittedBy=N(actor);
        SubmittedAtUtc=at;
    }
    public void Approve(string? actor, DateTimeOffset at)
    {
        if (Status!=EmployeeAdjustmentStatus.PendingApproval)throw new DomainException("Only pending adjustments can be approved.");
        Status=EmployeeAdjustmentStatus.Approved;
        ApprovedBy=N(actor);
        ApprovedAtUtc=at;
    }
    public void Reject(string reason, string? actor, DateTimeOffset at)
    {
        if (Status!=EmployeeAdjustmentStatus.PendingApproval||string.IsNullOrWhiteSpace(reason))throw new DomainException("Adjustment rejection is invalid.");
        Status=EmployeeAdjustmentStatus.Rejected;
        RejectedBy=N(actor);
        RejectedAtUtc=at;
        RejectionReason=reason.Trim();
    }
    public void Cancel(string reason, string? actor, DateTimeOffset at)
    {
        if (Status==EmployeeAdjustmentStatus.AppliedToPayroll)throw new DomainException("Applied adjustments cannot be cancelled.");
        if (string.IsNullOrWhiteSpace(reason))throw new DomainException("Cancellation reason is required.");
        Status=EmployeeAdjustmentStatus.Cancelled;
        CancelledBy=N(actor);
        CancelledAtUtc=at;
        CancellationReason=reason.Trim();
    }
    public void MarkAppliedToPayroll(Guid payrollId)
    {
        if (Status!=EmployeeAdjustmentStatus.Approved||payrollId==Guid.Empty)throw new DomainException("Only approved adjustments can be applied to payroll.");
        EmployeePayrollId=payrollId;
        Status=EmployeeAdjustmentStatus.AppliedToPayroll;
    }
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
}
