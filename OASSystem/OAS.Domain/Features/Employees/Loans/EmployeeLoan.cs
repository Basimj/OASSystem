using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Loans;

public sealed class EmployeeLoan : AuditableEntity<Guid>
{
    private EmployeeLoan()
    {
    }
    private EmployeeLoan(Guid id, string code, Guid emp, Guid? contractId, Guid? salaryId, DateOnly date, Guid currencyId, string currencyCode, string? symbol, byte decimals, decimal principal, int count, DateOnly firstDue, LoanRepaymentMode mode, string? reason)
    {
        Id=id;
        LoanCode=code.Trim();
        EmployeeId=emp;
        ContractId=contractId;
        SalaryStructureId=salaryId;
        LoanDate=date;
        CurrencyId=currencyId;
        CurrencyCodeSnapshot=currencyCode.Trim().ToUpperInvariant();
        CurrencySymbolSnapshot=N(symbol);
        CurrencyDecimalPlacesSnapshot=decimals;
        Status=EmployeeLoanStatus.Draft;
        UpdateDraft(principal, count, firstDue, mode, reason);
    }
    public string LoanCode { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid? ContractId { get; private set; }
    public Guid? SalaryStructureId { get; private set; }
    public DateOnly LoanDate { get; private set; }
    public Guid CurrencyId { get; private set; }
    public string CurrencyCodeSnapshot { get; private set; } = string.Empty;
    public string? CurrencySymbolSnapshot { get; private set; }
    public byte CurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal PrincipalAmount { get; private set; }
    public int InstallmentCount { get; private set; }
    public DateOnly FirstInstallmentDate { get; private set; }
    public LoanRepaymentMode RepaymentMode { get; private set; }
    public EmployeeLoanStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public Guid? PaymentVoucherId { get; private set; }
    public string? SubmittedBy { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public string? DisbursedBy { get; private set; }
    public DateTimeOffset? DisbursedAtUtc { get; private set; }
    public string? RejectedBy { get; private set; }
    public DateTimeOffset? RejectedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? CancelledBy { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static EmployeeLoan Create(Guid id, string code, Guid emp, Guid? contractId, Guid? salaryId, DateOnly date, Guid currencyId, string currencyCode, string? symbol, byte decimals, decimal principal, int count, DateOnly firstDue, LoanRepaymentMode mode, string? reason)
    {
        if (id==Guid.Empty||emp==Guid.Empty||currencyId==Guid.Empty||string.IsNullOrWhiteSpace(code)||string.IsNullOrWhiteSpace(currencyCode))throw new DomainException("Loan identity and currency are required.");
        if (decimals>6)throw new DomainException("Currency decimal places are invalid.");
        return new(id, code, emp, contractId, salaryId, date, currencyId, currencyCode, symbol, decimals, principal, count, firstDue, mode, reason);
    }
    public void UpdateDraft(decimal principal, int count, DateOnly firstDue, LoanRepaymentMode mode, string? reason)
    {
        if (Status!=EmployeeLoanStatus.Draft)throw new DomainException("Only draft loans can be edited.");
        if (principal<=0||count<=0)throw new DomainException("Loan principal and installment count must be positive.");
        if (!Enum.IsDefined(mode))throw new DomainException("Loan repayment mode is invalid.");
        PrincipalAmount=principal;
        InstallmentCount=count;
        FirstInstallmentDate=firstDue;
        RepaymentMode=mode;
        Reason=N(reason);
        if (Reason is
        {
            Length:>500
        }
        )throw new DomainException("Loan reason cannot exceed 500 characters.");
    }
    public void Submit(string? actor, DateTimeOffset at)
    {
        if (Status!=EmployeeLoanStatus.Draft)throw new DomainException("Only draft loans can be submitted.");
        Status=EmployeeLoanStatus.PendingApproval;
        SubmittedBy=N(actor);
        SubmittedAtUtc=at;
    }
    public void Approve(string? actor, DateTimeOffset at)
    {
        if (Status!=EmployeeLoanStatus.PendingApproval)throw new DomainException("Only pending loans can be approved.");
        Status=EmployeeLoanStatus.Approved;
        ApprovedBy=N(actor);
        ApprovedAtUtc=at;
    }
    public void Reject(string reason, string? actor, DateTimeOffset at)
    {
        if (Status!=EmployeeLoanStatus.PendingApproval||string.IsNullOrWhiteSpace(reason))throw new DomainException("Loan rejection is invalid.");
        RejectedBy=N(actor);
        RejectedAtUtc=at;
        RejectionReason=reason.Trim();
        Status=EmployeeLoanStatus.Rejected;
    }
    public void MarkDisbursed(Guid voucherId, string? actor, DateTimeOffset at)
    {
        if (Status!=EmployeeLoanStatus.Approved||voucherId==Guid.Empty)throw new DomainException("Only approved loans can be disbursed.");
        PaymentVoucherId=voucherId;
        DisbursedBy=N(actor);
        DisbursedAtUtc=at;
        Status=EmployeeLoanStatus.Active;
    }
    public void Cancel(string reason, string? actor, DateTimeOffset at)
    {
        if (Status is EmployeeLoanStatus.Active or EmployeeLoanStatus.Completed)throw new DomainException("Disbursed or completed loans cannot be cancelled.");
        if (string.IsNullOrWhiteSpace(reason))throw new DomainException("Cancellation reason is required.");
        CancelledBy=N(actor);
        CancelledAtUtc=at;
        CancellationReason=reason.Trim();
        Status=EmployeeLoanStatus.Cancelled;
    }
    public void MarkCompleted()
    {
        if (Status!=EmployeeLoanStatus.Active)throw new DomainException("Only active loans can be completed.");
        Status=EmployeeLoanStatus.Completed;
    }
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
}
