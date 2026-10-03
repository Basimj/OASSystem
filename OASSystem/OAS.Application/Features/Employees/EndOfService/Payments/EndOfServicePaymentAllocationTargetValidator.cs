using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Features.Employees.EndOfService;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Application.Features.Employees.EndOfService.Payments;

public sealed class EndOfServicePaymentAllocationTargetValidator(
    IReadRepository<EndOfServiceSettlement, Guid> settlements,
    IReadRepository<PaymentAllocation, Guid> allocations,
    OAS.Application.Features.Employees.Abstractions.IEmployeeHrOperationLock gate) : IPaymentAllocationTargetValidator
{
    public AllocationTargetDocumentType TargetDocumentType => AllocationTargetDocumentType.EndOfServiceSettlement;

    public async Task<PaymentAllocationTargetValidation> ValidateAsync(
        Guid targetDocumentId,
        Guid sourceCurrencyId,
        decimal allocatedAmount,
        decimal sourceBaseAllocatedAmount,
        Guid? excludingAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        var settlement = await settlements.GetByIdAsync(targetDocumentId, cancellationToken)
            ?? throw new NotFoundException(nameof(EndOfServiceSettlement), targetDocumentId);
        await gate.AcquireAsync(settlement.EmployeeId, cancellationToken);
        if (settlement.Status is not (EndOfServiceStatus.Posted or EndOfServiceStatus.Paid))
            throw new ConflictException("eos_not_posted", "End-of-service payment can only be allocated to a posted settlement.");
        if (settlement.CurrencyId != sourceCurrencyId)
            throw new ConflictException("eos_payment_currency_mismatch", "End-of-service payment currency must match settlement currency.");

        var existing = await allocations.ListAsync(new Specification<PaymentAllocation>().Where(x =>
            x.TargetDocumentType == AllocationTargetDocumentType.EndOfServiceSettlement &&
            x.TargetDocumentId == targetDocumentId), cancellationToken);
        if (excludingAllocationId.HasValue)
            existing = existing.Where(x => x.Id != excludingAllocationId.Value).ToArray();

        var already = existing.Sum(x => x.AllocatedAmount);
        var outstanding = Math.Max(0m, settlement.NetSettlementAmount - already);
        if (allocatedAmount <= 0m || allocatedAmount > outstanding)
            throw new ConflictException("eos_overpayment", "End-of-service payment exceeds the outstanding settlement amount.");

        var alreadyBase = existing.Sum(x => x.TargetBaseAllocatedAmount ?? x.BaseAllocatedAmount ?? 0m);
        var baseOutstanding = Math.Max(0m, settlement.BaseNetSettlementAmount - alreadyBase);
        var targetBase = allocatedAmount == outstanding
            ? baseOutstanding
            : Math.Round(settlement.BaseNetSettlementAmount * (allocatedAmount / settlement.NetSettlementAmount), 4, MidpointRounding.AwayFromZero);
        targetBase = Math.Min(targetBase, baseOutstanding);
        if (targetBase <= 0m)
            throw new ConflictException("eos_base_amount_invalid", "End-of-service base carrying amount is not available for payment allocation.");
        return new PaymentAllocationTargetValidation(targetBase);
    }
}
