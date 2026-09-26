using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
namespace OAS.Application.Accounting.PaymentAllocations.Commands.UpdatePaymentAllocation;
public sealed class UpdatePaymentAllocationCommandHandler(
    IRepository<PaymentAllocation, Guid> repository,
    IReadRepository<ReceiptVoucherLine, Guid> receiptLines,
    IReadRepository<PaymentVoucherLine, Guid> paymentLines) : IRequestHandler<UpdatePaymentAllocationCommand>
{
    public async Task Handle(UpdatePaymentAllocationCommand request, CancellationToken ct)
    {
        var allocation = await repository.GetForUpdateAsync(request.Id, ct) ?? throw new NotFoundException(nameof(PaymentAllocation), request.Id);
        if (!allocation.ReceiptVoucherLineId.HasValue && !allocation.PaymentVoucherLineId.HasValue)
            throw new ConflictException("legacy_allocation_readonly", "Legacy payment allocations must be migrated before they can be edited.");
        var sourceAmount = await GetSourceAmountAsync(allocation, ct);
        var existing = await repository.ListAsync(new Specification<PaymentAllocation>().Where(x =>
            allocation.ReceiptVoucherLineId.HasValue ? x.ReceiptVoucherLineId == allocation.ReceiptVoucherLineId : x.PaymentVoucherLineId == allocation.PaymentVoucherLineId), ct);
        var available = sourceAmount - existing.Where(x => x.Id != allocation.Id).Sum(x => x.AllocatedAmount);
        if (request.Data.AllocatedAmount > available)
            throw new ConflictException("payment_allocation_exceeds_available_amount", $"The requested allocation ({request.Data.AllocatedAmount}) exceeds the available source-line amount ({available}).");
        var rate = allocation.ExchangeRate ?? 1m;
        allocation.UpdateAllocatedAmount(request.Data.AllocatedAmount, Math.Round(request.Data.AllocatedAmount * rate, 4, MidpointRounding.AwayFromZero));
        repository.Update(allocation);
    }
    private async Task<decimal> GetSourceAmountAsync(PaymentAllocation a, CancellationToken ct)
    {
        if (a.ReceiptVoucherLineId is Guid rid) return (await receiptLines.GetByIdAsync(rid, ct) ?? throw new NotFoundException(nameof(ReceiptVoucherLine), rid)).Amount;
        if (a.PaymentVoucherLineId is Guid pid) return (await paymentLines.GetByIdAsync(pid, ct) ?? throw new NotFoundException(nameof(PaymentVoucherLine), pid)).Amount;
        throw new ConflictException("payment_source_line_required", "Payment source line is missing.");
    }
}
