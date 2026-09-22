using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainAllocationTargetDocumentType = OAS.Domain.Accounting.Enums.AllocationTargetDocumentType;
using DomainPaymentSourceType = OAS.Domain.Accounting.Enums.PaymentSourceType;

namespace OAS.Application.Accounting.PaymentAllocations.Commands.CreatePaymentAllocation;

public sealed class CreatePaymentAllocationCommandHandler(
    IRepository<PaymentAllocation, Guid> repository,
    IRepository<ReceiptVoucher, Guid> receiptVoucherRepository,
    IRepository<PaymentVoucher, Guid> paymentVoucherRepository,
    TimeProvider timeProvider)
    : IRequestHandler<CreatePaymentAllocationCommand, Guid>
{
    public async Task<Guid> Handle(
        CreatePaymentAllocationCommand request,
        CancellationToken cancellationToken)
    {
        var data = request.Data;
        var sourceType = (DomainPaymentSourceType)(int)data.PaymentSourceType;
        var sourceAmount = await GetSourceAmountAsync(
            sourceType, data.PaymentSourceId, cancellationToken);

        var existingAllocations = await repository.ListAsync(
            new Specification<PaymentAllocation>()
                .Where(x => x.PaymentSourceType == sourceType &&
                            x.PaymentSourceId == data.PaymentSourceId),
            cancellationToken);

        var alreadyAllocated = existingAllocations.Sum(x => x.AllocatedAmount);
        var availableAmount = sourceAmount - alreadyAllocated;

        if (data.AllocatedAmount > availableAmount)
        {
            throw new ConflictException(
                "payment_allocation_exceeds_available_amount",
                $"The requested allocation ({data.AllocatedAmount}) exceeds the available payment amount ({availableAmount}).");
        }

        var entity = PaymentAllocation.Create(
            Guid.NewGuid(),
            sourceType,
            data.PaymentSourceId,
            (DomainAllocationTargetDocumentType)(int)data.TargetDocumentType,
            data.TargetDocumentId,
            data.AllocatedAmount,
            timeProvider.GetUtcNow().UtcDateTime);

        await repository.AddAsync(entity, cancellationToken);
        return entity.Id;
    }

    private async Task<decimal> GetSourceAmountAsync(
        DomainPaymentSourceType sourceType,
        Guid sourceId,
        CancellationToken cancellationToken)
    {
        switch (sourceType)
        {
            case DomainPaymentSourceType.ReceiptVoucher:
                var receipt = await receiptVoucherRepository.GetForUpdateAsync(sourceId, cancellationToken);
                if (receipt is null)
                    throw new NotFoundException(nameof(ReceiptVoucher), sourceId);
                // Touch the source aggregate so its rowversion acts as an optimistic
                // concurrency guard against two allocations overspending the same source.
                receiptVoucherRepository.Update(receipt);
                return receipt.TotalAmount;

            case DomainPaymentSourceType.PaymentVoucher:
                var payment = await paymentVoucherRepository.GetForUpdateAsync(sourceId, cancellationToken);
                if (payment is null)
                    throw new NotFoundException(nameof(PaymentVoucher), sourceId);
                paymentVoucherRepository.Update(payment);
                return payment.TotalAmount;

            case DomainPaymentSourceType.CustomerAdvance:
                // The task defines CustomerAdvance as a source type, but this codebase has no
                // CustomerAdvance aggregate or application port that can supply its balance.
                // Rejecting it here is safer than allowing an unbounded allocation.
                throw new ConflictException(
                    "customer_advance_source_unavailable",
                    "Customer advance balance cannot be validated because no customer-advance source is available in the current Accounting core.");

            default:
                throw new ConflictException(
                    "payment_source_type_invalid",
                    $"Payment source type '{sourceType}' is not supported.");
        }
    }
}
