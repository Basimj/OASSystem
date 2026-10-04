using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Loans;

public sealed class EmployeeLoanInstallment : AuditableEntity<Guid>
{
    private EmployeeLoanInstallment()
    {
    }
    private EmployeeLoanInstallment(Guid id, Guid loan, int seq, DateOnly due, decimal amount)
    {
        Id=id;
        EmployeeLoanId=loan;
        InstallmentSequence=seq;
        DueDate=due;
        Amount=amount;
        Status=LoanInstallmentStatus.Pending;
    }
    public Guid EmployeeLoanId { get; private set; }
    public int InstallmentSequence { get; private set; }
    public DateOnly DueDate { get; private set; }
    public decimal Amount { get; private set; }
    public LoanInstallmentStatus Status { get; private set; }
    public Guid? EmployeePayrollId { get; private set; }
    public Guid? ReceiptVoucherId { get; private set; }
    public Guid? EndOfServiceSettlementId { get; private set; }
    public DateTimeOffset? DeductedAtUtc { get; private set; }
    public DateTimeOffset? ExternallyPaidAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static EmployeeLoanInstallment Create(Guid id, Guid loan, int seq, DateOnly due, decimal amount)
    {
        if (id==Guid.Empty||loan==Guid.Empty||seq<=0||amount<=0)throw new DomainException("Loan installment data is invalid.");
        return new(id, loan, seq, due, amount);
    }
    public void Schedule()
    {
        if (Status!=LoanInstallmentStatus.Pending)throw new DomainException("Only pending installments can be scheduled.");
        Status=LoanInstallmentStatus.Scheduled;
    }
    public void MarkDeducted(Guid payrollId, DateTimeOffset at)
    {
        if (Status!=LoanInstallmentStatus.Scheduled||payrollId==Guid.Empty)throw new DomainException("Only scheduled installments can be deducted.");
        EmployeePayrollId=payrollId;
        DeductedAtUtc=at;
        Status=LoanInstallmentStatus.Deducted;
    }
    public void MarkDeductedAtEndOfService(Guid settlementId, DateTimeOffset at)
    {
        if (Status!=LoanInstallmentStatus.Scheduled||settlementId==Guid.Empty)throw new DomainException("Only scheduled installments can be settled at end of service.");
        EndOfServiceSettlementId=settlementId;
        DeductedAtUtc=at;
        Status=LoanInstallmentStatus.DeductedAtEndOfService;
    }
    public void MarkPaidExternally(Guid receiptId, DateTimeOffset at)
    {
        if (Status!=LoanInstallmentStatus.Scheduled||receiptId==Guid.Empty)throw new DomainException("Only scheduled installments can be externally paid.");
        ReceiptVoucherId=receiptId;
        ExternallyPaidAtUtc=at;
        Status=LoanInstallmentStatus.PaidExternally;
    }
    public void Cancel()
    {
        if (Status is LoanInstallmentStatus.Deducted or LoanInstallmentStatus.PaidExternally or LoanInstallmentStatus.DeductedAtEndOfService)throw new DomainException("Paid installments cannot be cancelled.");
        Status=LoanInstallmentStatus.Cancelled;
    }
}
