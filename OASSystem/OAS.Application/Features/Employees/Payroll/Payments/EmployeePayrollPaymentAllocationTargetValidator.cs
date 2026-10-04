using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Features.Employees.Enums;
using OAS.Domain.Features.Employees.Payroll;

namespace OAS.Application.Features.Employees.Payroll.Payments;

public sealed class EmployeePayrollPaymentAllocationTargetValidator(
    IReadRepository<EmployeePayroll, Guid> payrolls,
    IReadRepository<PaymentAllocation, Guid> allocations,
    OAS.Application.Features.Employees.Abstractions.IEmployeeHrOperationLock gate) : IPaymentAllocationTargetValidator
{
    public AllocationTargetDocumentType TargetDocumentType => AllocationTargetDocumentType.EmployeePayroll;

    public async Task<PaymentAllocationTargetValidation> ValidateAsync(
        Guid targetDocumentId,
        Guid sourceCurrencyId,
        decimal allocatedAmount,
        decimal sourceBaseAllocatedAmount,
        Guid? excludingAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        var payroll = await payrolls.GetByIdAsync(targetDocumentId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeePayroll), targetDocumentId);
        await gate.AcquireAsync(payroll.EmployeeId, cancellationToken);
        if (payroll.Status != EmployeePayrollStatus.Posted)
            throw new ConflictException("employee_payroll_not_posted", "Salary payment can only be allocated to a posted employee payroll.");
        if (payroll.CurrencyId != sourceCurrencyId)
            throw new ConflictException("employee_payroll_payment_currency_mismatch", "Salary payment currency must match the employee payroll currency.");
        if (allocatedAmount <= 0)
            throw new ConflictException("employee_payroll_payment_amount_invalid", "Salary payment amount must be greater than zero.");

        var existing = await allocations.ListAsync(
            new Specification<PaymentAllocation>().Where(x =>
                x.TargetDocumentType == AllocationTargetDocumentType.EmployeePayroll &&
                x.TargetDocumentId == targetDocumentId), cancellationToken);
        if (excludingAllocationId.HasValue)
            existing = existing.Where(x => x.Id != excludingAllocationId.Value).ToArray();

        var already = existing.Sum(x => x.AllocatedAmount);
        var outstanding = Math.Max(0m, payroll.NetPay - already);
        if (allocatedAmount > outstanding)
            throw new ConflictException("employee_payroll_overpayment", "Salary payment exceeds the employee payroll outstanding amount.");

        var alreadyTargetBase = existing.Sum(x => x.TargetBaseAllocatedAmount ?? x.BaseAllocatedAmount ?? 0m);
        var baseOutstanding = Math.Max(0m, payroll.BaseNetPay - alreadyTargetBase);
        decimal targetBase;
        if (allocatedAmount == outstanding)
        {
            targetBase = baseOutstanding;
        }
        else if (payroll.NetPay == 0m)
        {
            targetBase = 0m;
        }
        else
        {
            targetBase = Math.Round(payroll.BaseNetPay * (allocatedAmount / payroll.NetPay), 4, MidpointRounding.AwayFromZero);
            targetBase = Math.Min(targetBase, baseOutstanding);
        }

        if (targetBase <= 0m && allocatedAmount > 0m)
            throw new ConflictException("employee_payroll_base_amount_invalid", "Employee payroll base carrying amount is not available for payment allocation.");

        return new PaymentAllocationTargetValidation(targetBase);
    }
}
