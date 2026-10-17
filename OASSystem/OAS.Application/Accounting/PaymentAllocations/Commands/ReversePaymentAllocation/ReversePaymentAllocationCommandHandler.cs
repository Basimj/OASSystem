using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Contracts.Accounting.PaymentAllocations;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.PaymentAllocations.Commands.ReversePaymentAllocation;

public sealed class ReversePaymentAllocationCommandHandler(
    IRepository<PaymentAllocation, Guid> allocations,
    IReadRepository<PaymentVoucherLine, Guid> paymentLines,
    IReadRepository<PaymentVoucher, Guid> paymentVouchers,
    IEnumerable<IPaymentAllocationSourceTargetValidator> sourceTargetValidators,
    IPurchasePaymentAllocationConcurrencyPort allocationConcurrency,
    ISupplierSettlementFxPostingService supplierSettlementFxPosting,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<ReversePaymentAllocationCommand, ReversePaymentAllocationResultDto>
{
    public async Task<ReversePaymentAllocationResultDto> Handle(
        ReversePaymentAllocationCommand request,
        CancellationToken ct)
    {
        var initial = await allocations.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(PaymentAllocation), request.Id);

        if (initial.TargetDocumentType != AllocationTargetDocumentType.PurchaseInvoice)
            throw new ConflictException(
                "payment_allocation_reversal_scope_invalid",
                "هذا المسار يعكس تخصيصات فواتير المورد فقط.");
        if (initial.PaymentVoucherLineId is not Guid paymentLineId || paymentLineId == Guid.Empty)
            throw new ConflictException(
                "payment_allocation_reversal_source_invalid",
                "تخصيص فاتورة المورد يجب أن يكون مرتبطًا بسطر سند صرف.");

        await allocationConcurrency.LockPaymentLineAndInvoiceAsync(
            paymentLineId,
            initial.TargetDocumentId,
            ct);

        var allocation = await allocations.GetForUpdateAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(PaymentAllocation), request.Id);

        if (!Guid.TryParse(currentUser.UserId, out var userId) || userId == Guid.Empty)
            throw new ForbiddenException();

        var paymentLine = await paymentLines.GetByIdAsync(paymentLineId, ct)
            ?? throw new NotFoundException(nameof(PaymentVoucherLine), paymentLineId);
        var voucher = await paymentVouchers.GetByIdAsync(paymentLine.PaymentVoucherId, ct)
            ?? throw new NotFoundException(nameof(PaymentVoucher), paymentLine.PaymentVoucherId);
        if (voucher.Status != PaymentVoucherStatus.Posted)
            throw new ConflictException(
                "payment_allocation_reversal_source_not_posted",
                "لا يمكن عكس تخصيص مرتبط بسند صرف غير مرحل.");

        var validator = sourceTargetValidators.SingleOrDefault(
            x => x.TargetDocumentType == AllocationTargetDocumentType.PurchaseInvoice)
            ?? throw new ConflictException(
                "purchase_invoice_payment_validator_missing",
                "إعدادات التحقق من تخصيص فواتير المورد غير متاحة.");

        await validator.ValidateSourceAsync(
            allocation.TargetDocumentId,
            new PaymentAllocationSourceContext(
                PaymentSourceType.PaymentVoucher,
                paymentLine.PartyType,
                paymentLine.CustomerId,
                paymentLine.SupplierId,
                paymentLine.EmployeeId),
            ct);

        // Replaying the same reversal is safe and must never generate a second journal.
        if (allocation.IsReversed)
        {
            var replayFx = await supplierSettlementFxPosting.ReverseAsync(
                new SupplierSettlementFxReversalRequest(
                    allocation.Id,
                    request.Data.ReversalDate,
                    $"إعادة طلب عكس فرق صرف لتخصيص {allocation.Id}"),
                userId,
                timeProvider.GetUtcNow().UtcDateTime,
                ct);

            return new ReversePaymentAllocationResultDto(
                allocation.Id,
                replayFx.OriginalJournalEntryId,
                replayFx.ReversalJournalEntryId,
                true);
        }

        EnsureRowVersionMatches(allocation, request.Data.RowVersion);
        if (string.IsNullOrWhiteSpace(request.Data.Reason))
            throw new ConflictException(
                "payment_allocation_reversal_reason_required",
                "سبب عكس التخصيص مطلوب.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var fx = await supplierSettlementFxPosting.ReverseAsync(
            new SupplierSettlementFxReversalRequest(
                allocation.Id,
                request.Data.ReversalDate,
                $"عكس فرق الصرف لتخصيص فاتورة مورد: {request.Data.Reason.Trim()}"),
            userId,
            now,
            ct);

        allocation.Reverse(
            now,
            currentUser.UserId ?? userId.ToString("D"),
            request.Data.Reason);
        allocations.Update(allocation);

        return new ReversePaymentAllocationResultDto(
            allocation.Id,
            fx.OriginalJournalEntryId,
            fx.ReversalJournalEntryId,
            false);
    }

    private static void EnsureRowVersionMatches(PaymentAllocation allocation, string rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
            throw new ConflictException(
                "payment_allocation_rowversion_required",
                "نسخة التزامن للتخصيص مطلوبة قبل العكس.");

        string current;
        try
        {
            current = Convert.ToBase64String(allocation.RowVersion);
        }
        catch
        {
            throw new ConflictException(
                "payment_allocation_rowversion_invalid",
                "تعذر التحقق من نسخة التزامن للتخصيص.");
        }

        if (!string.Equals(current, rowVersion.Trim(), StringComparison.Ordinal))
            throw new ConflictException(
                "payment_allocation_concurrency_conflict",
                "تم تعديل التخصيص بواسطة عملية أخرى. أعد تحميل البيانات ثم حاول مرة أخرى.");
    }
}
