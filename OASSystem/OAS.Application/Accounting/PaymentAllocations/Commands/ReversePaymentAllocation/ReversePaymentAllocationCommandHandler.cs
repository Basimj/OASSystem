using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Sales.Abstractions;
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
    TimeProvider timeProvider,
    IReadRepository<ReceiptVoucherLine, Guid>? receiptLines = null,
    IReadRepository<ReceiptVoucher, Guid>? receiptVouchers = null,
    ISalesPaymentAllocationConcurrencyPort? salesAllocationConcurrency = null,
    ICustomerSettlementFxPostingService? customerSettlementFxPosting = null)
    : IRequestHandler<ReversePaymentAllocationCommand, ReversePaymentAllocationResultDto>
{
    public async Task<ReversePaymentAllocationResultDto> Handle(
        ReversePaymentAllocationCommand request,
        CancellationToken ct)
    {
        var initial = await allocations.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(PaymentAllocation), request.Id);

        await LockTargetAsync(initial, ct);

        var allocation = await allocations.GetForUpdateAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(PaymentAllocation), request.Id);

        var userId = GetCurrentUserId();
        await ValidateSourceAsync(allocation, ct);

        if (allocation.IsReversed)
        {
            var replayFx = await ReverseFxAsync(
                allocation,
                request.Data.ReversalDate,
                userId,
                timeProvider.GetUtcNow().UtcDateTime,
                true,
                null,
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
        var fx = await ReverseFxAsync(
            allocation,
            request.Data.ReversalDate,
            userId,
            now,
            false,
            request.Data.Reason,
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

    private async Task LockTargetAsync(PaymentAllocation allocation, CancellationToken ct)
    {
        if (allocation.TargetDocumentType == AllocationTargetDocumentType.PurchaseInvoice)
        {
            if (allocation.PaymentVoucherLineId is not Guid paymentLineId || paymentLineId == Guid.Empty)
                throw new ConflictException(
                    "payment_allocation_reversal_source_invalid",
                    "تخصيص فاتورة المورد يجب أن يكون مرتبطًا بسطر سند صرف.");

            await allocationConcurrency.LockPaymentLineAndInvoiceAsync(
                paymentLineId,
                allocation.TargetDocumentId,
                ct);
            return;
        }

        if (allocation.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice)
        {
            if (salesAllocationConcurrency is null)
                throw new ConflictException(
                    "sales_payment_allocation_concurrency_missing",
                    "خدمة حماية تزامن تخصيصات المبيعات غير متاحة.");
            if (allocation.ReceiptVoucherLineId is not Guid receiptLineId || receiptLineId == Guid.Empty)
                throw new ConflictException(
                    "sales_payment_allocation_reversal_source_invalid",
                    "تخصيص فاتورة المبيعات يجب أن يكون مرتبطًا بسطر سند قبض.");

            await salesAllocationConcurrency.LockReceiptLineAndInvoiceAsync(
                receiptLineId,
                allocation.TargetDocumentId,
                ct);
            return;
        }

        throw new ConflictException(
            "payment_allocation_reversal_scope_invalid",
            "يمكن عكس تخصيصات فواتير المورد أو فواتير المبيعات من هذا المسار فقط.");
    }

    private async Task ValidateSourceAsync(PaymentAllocation allocation, CancellationToken ct)
    {
        if (allocation.TargetDocumentType == AllocationTargetDocumentType.PurchaseInvoice)
        {
            var paymentLineId = allocation.PaymentVoucherLineId!.Value;
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
            return;
        }

        if (receiptLines is null || receiptVouchers is null)
            throw new ConflictException(
                "sales_payment_allocation_source_services_missing",
                "خدمات قراءة سندات القبض المطلوبة لعكس تخصيص المبيعات غير متاحة.");

        var receiptLineId = allocation.ReceiptVoucherLineId!.Value;
        var receiptLine = await receiptLines.GetByIdAsync(receiptLineId, ct)
            ?? throw new NotFoundException(nameof(ReceiptVoucherLine), receiptLineId);
        var receiptVoucher = await receiptVouchers.GetByIdAsync(receiptLine.ReceiptVoucherId, ct)
            ?? throw new NotFoundException(nameof(ReceiptVoucher), receiptLine.ReceiptVoucherId);
        if (receiptVoucher.Status != ReceiptVoucherStatus.Posted)
            throw new ConflictException(
                "payment_allocation_reversal_source_not_posted",
                "لا يمكن عكس تخصيص مرتبط بسند قبض غير مرحل.");

        var salesValidator = sourceTargetValidators.SingleOrDefault(
            x => x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice)
            ?? throw new ConflictException(
                "sales_invoice_payment_validator_missing",
                "إعدادات التحقق من تخصيص فواتير المبيعات غير متاحة.");

        await salesValidator.ValidateSourceAsync(
            allocation.TargetDocumentId,
            new PaymentAllocationSourceContext(
                PaymentSourceType.ReceiptVoucher,
                receiptLine.PartyType,
                receiptLine.CustomerId,
                receiptLine.SupplierId,
                receiptLine.EmployeeId),
            ct);
    }

    private async Task<FxReverseResult> ReverseFxAsync(
        PaymentAllocation allocation,
        DateOnly reversalDate,
        Guid userId,
        DateTime now,
        bool replay,
        string? reason,
        CancellationToken ct)
    {
        if (allocation.TargetDocumentType == AllocationTargetDocumentType.PurchaseInvoice)
        {
            var result = await supplierSettlementFxPosting.ReverseAsync(
                new SupplierSettlementFxReversalRequest(
                    allocation.Id,
                    reversalDate,
                    replay
                        ? $"إعادة طلب عكس فرق صرف لتخصيص {allocation.Id}"
                        : $"عكس فرق الصرف لتخصيص فاتورة مورد: {reason!.Trim()}"),
                userId,
                now,
                ct);
            return new FxReverseResult(result.OriginalJournalEntryId, result.ReversalJournalEntryId);
        }

        if (customerSettlementFxPosting is null)
            throw new ConflictException(
                "customer_settlement_fx_service_missing",
                "خدمة عكس فرق الصرف لتحصيلات العملاء غير متاحة.");

        var customerResult = await customerSettlementFxPosting.ReverseAsync(
            new CustomerSettlementFxReversalRequest(
                allocation.Id,
                reversalDate,
                replay
                    ? $"إعادة طلب عكس فرق صرف لتحصيل {allocation.Id}"
                    : $"عكس فرق الصرف لتحصيل فاتورة مبيعات: {reason!.Trim()}"),
            userId,
            now,
            ct);
        return new FxReverseResult(
            customerResult.OriginalJournalEntryId,
            customerResult.ReversalJournalEntryId);
    }

    private Guid GetCurrentUserId()
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId) || userId == Guid.Empty)
            throw new ForbiddenException();
        return userId;
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

    private sealed record FxReverseResult(
        Guid? OriginalJournalEntryId,
        Guid? ReversalJournalEntryId);
}
