using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainPaymentSourceType = OAS.Domain.Accounting.Enums.PaymentSourceType;

namespace OAS.Application.Accounting.PaymentAllocations.Commands.UpdatePaymentAllocation;

public sealed class UpdatePaymentAllocationCommandHandler(
    IRepository<PaymentAllocation, Guid> repository,
    IRepository<ReceiptVoucher, Guid> receiptVoucherRepository,
    IRepository<PaymentVoucher, Guid> paymentVoucherRepository)
    : IRequestHandler<UpdatePaymentAllocationCommand>
{
    public async Task Handle(
        UpdatePaymentAllocationCommand request,
        CancellationToken cancellationToken)
    {
        var allocation = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (allocation is null)
            throw new NotFoundException(nameof(PaymentAllocation), request.Id);

        var sourceAmount = await GetSourceAmountAsync(
            allocation.PaymentSourceType,
            allocation.PaymentSourceId,
            cancellationToken);

        var sourceAllocations = await repository.ListAsync(
            new Specification<PaymentAllocation>()
                .Where(x => x.PaymentSourceType == allocation.PaymentSourceType &&
                            x.PaymentSourceId == allocation.PaymentSourceId),
            cancellationToken);

        var allocatedByOthers = sourceAllocations
            .Where(x => x.Id != allocation.Id)
            .Sum(x => x.AllocatedAmount);

        var availableAmount = sourceAmount - allocatedByOthers;
        if (request.Data.AllocatedAmount > availableAmount)
        {
            throw new ConflictException(
                "payment_allocation_exceeds_available_amount",
                $"The requested allocation ({request.Data.AllocatedAmount}) exceeds the available payment amount ({availableAmount}).");
        }

        allocation.UpdateAllocatedAmount(request.Data.AllocatedAmount);
        repository.Update(allocation);
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
                receiptVoucherRepository.Update(receipt);
                return receipt.TotalAmount;

            case DomainPaymentSourceType.PaymentVoucher:
                var payment = await paymentVoucherRepository.GetForUpdateAsync(sourceId, cancellationToken);
                if (payment is null)
                    throw new NotFoundException(nameof(PaymentVoucher), sourceId);
                paymentVoucherRepository.Update(payment);
                return payment.TotalAmount;

            case DomainPaymentSourceType.CustomerAdvance:
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
