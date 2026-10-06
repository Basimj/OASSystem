using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PaymentAllocations.Commands.UpdatePaymentAllocation;

public sealed class UpdatePaymentAllocationCommandHandler(
    IRepository<PaymentAllocation, Guid> repository,
    IReadRepository<ReceiptVoucherLine, Guid> receiptLines,
    IReadRepository<ReceiptVoucher, Guid> receiptVouchers,
    IReadRepository<PaymentVoucherLine, Guid> paymentLines,
    IReadRepository<PaymentVoucher, Guid> paymentVouchers,
    IEnumerable<IPaymentAllocationTargetValidator> targetValidators,
    IEnumerable<IPaymentAllocationSourceTargetValidator> sourceTargetValidators) : IRequestHandler<UpdatePaymentAllocationCommand>
{
    public async Task Handle(UpdatePaymentAllocationCommand request, CancellationToken ct)
    {
        var allocation = await repository.GetForUpdateAsync(request.Id, ct) ?? throw new NotFoundException(nameof(PaymentAllocation), request.Id);
        if (allocation.TargetDocumentType is OAS.Domain.Accounting.Enums.AllocationTargetDocumentType.EmployeePayroll or OAS.Domain.Accounting.Enums.AllocationTargetDocumentType.EndOfServiceSettlement)
            throw new ConflictException("managed_payment_allocation_immutable", "Payroll and end-of-service allocations are managed by their dedicated payment workflows and cannot be edited directly.");
        if (!allocation.ReceiptVoucherLineId.HasValue && !allocation.PaymentVoucherLineId.HasValue)
            throw new ConflictException("legacy_allocation_readonly", "Legacy payment allocations must be migrated before they can be edited.");

        var source = await GetSourceAsync(allocation, ct);
        var sourceAmount = source.Amount;
        var existing = await repository.ListAsync(new Specification<PaymentAllocation>().Where(x =>
            allocation.ReceiptVoucherLineId.HasValue ? x.ReceiptVoucherLineId == allocation.ReceiptVoucherLineId : x.PaymentVoucherLineId == allocation.PaymentVoucherLineId), ct);
        var available = sourceAmount - existing.Where(x => x.Id != allocation.Id).Sum(x => x.AllocatedAmount);
        if (request.Data.AllocatedAmount > available)
            throw new ConflictException("payment_allocation_exceeds_available_amount", $"The requested allocation ({request.Data.AllocatedAmount}) exceeds the available source-line amount ({available}).");

        var rate = allocation.ExchangeRate ?? 1m;
        var sourceBaseAmount = Math.Round(request.Data.AllocatedAmount * rate, 4, MidpointRounding.AwayFromZero);
        var sourceTargetValidator = sourceTargetValidators.SingleOrDefault(x => x.TargetDocumentType == allocation.TargetDocumentType);
        if (sourceTargetValidator is not null)
            await sourceTargetValidator.ValidateSourceAsync(allocation.TargetDocumentId, source.Context, ct);

        var targetBaseAmount = sourceBaseAmount;
        var validator = targetValidators.SingleOrDefault(x => x.TargetDocumentType == allocation.TargetDocumentType);
        if (validator is not null && allocation.CurrencyId.HasValue)
        {
            targetBaseAmount = (await validator.ValidateAsync(
                allocation.TargetDocumentId,
                allocation.CurrencyId.Value,
                request.Data.AllocatedAmount,
                sourceBaseAmount,
                allocation.Id,
                ct)).TargetBaseAllocatedAmount;
        }

        allocation.UpdateAllocatedAmount(request.Data.AllocatedAmount, sourceBaseAmount, targetBaseAmount);
        repository.Update(allocation);
    }

    private async Task<SourceInfo> GetSourceAsync(PaymentAllocation allocation, CancellationToken ct)
    {
        if (allocation.ReceiptVoucherLineId is Guid rid)
        {
            var line = await receiptLines.GetByIdAsync(rid, ct) ?? throw new NotFoundException(nameof(ReceiptVoucherLine), rid);
            var voucher = await receiptVouchers.GetByIdAsync(line.ReceiptVoucherId, ct) ?? throw new NotFoundException(nameof(ReceiptVoucher), line.ReceiptVoucherId);
            if (voucher.Status != OAS.Domain.Accounting.Enums.ReceiptVoucherStatus.Posted)
                throw new ConflictException("payment_allocation_receipt_not_posted", "لا يمكن تعديل تخصيص مرتبط بسند قبض غير مرحل.");
            return new SourceInfo(
                line.Amount,
                new PaymentAllocationSourceContext(
                    OAS.Domain.Accounting.Enums.PaymentSourceType.ReceiptVoucher,
                    line.PartyType,
                    line.CustomerId,
                    line.SupplierId,
                    line.EmployeeId));
        }

        if (allocation.PaymentVoucherLineId is Guid pid)
        {
            var line = await paymentLines.GetByIdAsync(pid, ct) ?? throw new NotFoundException(nameof(PaymentVoucherLine), pid);
            var voucher = await paymentVouchers.GetByIdAsync(line.PaymentVoucherId, ct) ?? throw new NotFoundException(nameof(PaymentVoucher), line.PaymentVoucherId);
            if (voucher.Status != OAS.Domain.Accounting.Enums.PaymentVoucherStatus.Posted)
                throw new ConflictException("payment_allocation_payment_not_posted", "لا يمكن تعديل تخصيص مرتبط بسند صرف غير مرحل.");
            return new SourceInfo(
                line.Amount,
                new PaymentAllocationSourceContext(
                    OAS.Domain.Accounting.Enums.PaymentSourceType.PaymentVoucher,
                    line.PartyType,
                    line.CustomerId,
                    line.SupplierId,
                    line.EmployeeId));
        }

        throw new ConflictException("payment_source_line_required", "Payment source line is missing.");
    }

    private sealed record SourceInfo(decimal Amount, PaymentAllocationSourceContext Context);
}
