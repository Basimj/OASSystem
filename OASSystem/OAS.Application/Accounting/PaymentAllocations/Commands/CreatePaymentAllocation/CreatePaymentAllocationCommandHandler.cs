using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Domain.Accounting.Entities;
using DomainAllocationTargetDocumentType = OAS.Domain.Accounting.Enums.AllocationTargetDocumentType;

namespace OAS.Application.Accounting.PaymentAllocations.Commands.CreatePaymentAllocation;

public sealed class CreatePaymentAllocationCommandHandler(
    IRepository<PaymentAllocation, Guid> repository,
    IReadRepository<ReceiptVoucherLine, Guid> receiptLines,
    IReadRepository<ReceiptVoucher, Guid> receiptVouchers,
    IReadRepository<PaymentVoucherLine, Guid> paymentLines,
    IReadRepository<PaymentVoucher, Guid> paymentVouchers,
    IEnumerable<IPaymentAllocationTargetValidator> targetValidators,
    IEnumerable<IPaymentAllocationSourceTargetValidator> sourceTargetValidators,
    ISupplierSettlementFxPostingService supplierSettlementFxPosting,
    IPurchasePaymentAllocationConcurrencyPort purchaseAllocationConcurrency,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<CreatePaymentAllocationCommand, Guid>
{
    public async Task<Guid> Handle(CreatePaymentAllocationCommand request, CancellationToken ct)
    {
        var d = request.Data;
        var targetType = (DomainAllocationTargetDocumentType)(byte)d.TargetDocumentType;
        if (targetType is DomainAllocationTargetDocumentType.EmployeePayroll or DomainAllocationTargetDocumentType.EndOfServiceSettlement)
            throw new ConflictException("managed_payment_allocation_required", "Payroll and end-of-service allocations can only be created by their dedicated payment workflows.");

        // AP allocations are line-owned. Lock source + target before any SUM/outstanding
        // read so concurrent HTTP requests cannot both consume the same remaining amount.
        if (targetType == DomainAllocationTargetDocumentType.PurchaseInvoice)
        {
            if (d.PaymentVoucherLineId is not Guid paymentLineId || paymentLineId == Guid.Empty ||
                d.ReceiptVoucherLineId.HasValue || d.CustomerAdvanceApplicationId.HasValue)
            {
                throw new ConflictException(
                    "purchase_invoice_payment_source_invalid",
                    "يمكن تخصيص فاتورة المورد من سطر سند صرف مرحل فقط.");
            }

            await purchaseAllocationConcurrency.LockPaymentLineAndInvoiceAsync(
                paymentLineId,
                d.TargetDocumentId,
                ct);
        }

        var idempotencyKey = NormalizeIdempotencyKey(d.IdempotencyKey);
        if (idempotencyKey is not null)
        {
            var replay = await FindByIdempotencyKeyAsync(idempotencyKey, ct);
            if (replay is not null)
            {
                EnsureReplayMatches(replay, d, targetType);
                return replay.Id;
            }
        }

        var source = await ResolveSourceAsync(d.ReceiptVoucherLineId, d.PaymentVoucherLineId, ct);
        var existing = await repository.ListAsync(new Specification<PaymentAllocation>().Where(x =>
            x.ReversedAtUtc == null &&
            (d.ReceiptVoucherLineId.HasValue
                ? x.ReceiptVoucherLineId == d.ReceiptVoucherLineId
                : x.PaymentVoucherLineId == d.PaymentVoucherLineId)), ct);
        var available = source.Amount - existing.Sum(x => x.AllocatedAmount);
        if (d.AllocatedAmount > available)
            throw new ConflictException("payment_allocation_exceeds_available_amount", $"The requested allocation ({d.AllocatedAmount}) exceeds the available source-line amount ({available}).");

        var sourceBaseAmount = Math.Round(d.AllocatedAmount * source.ExchangeRate, 4, MidpointRounding.AwayFromZero);
        var sourceTargetValidator = sourceTargetValidators.SingleOrDefault(x => x.TargetDocumentType == targetType);
        if (sourceTargetValidator is not null)
            await sourceTargetValidator.ValidateSourceAsync(d.TargetDocumentId, source.Context, ct);

        var validation = new PaymentAllocationTargetValidation(sourceBaseAmount);
        var validator = targetValidators.SingleOrDefault(x => x.TargetDocumentType == targetType);
        if (validator is not null)
        {
            validation = await validator.ValidateAsync(
                d.TargetDocumentId,
                source.CurrencyId,
                d.AllocatedAmount,
                sourceBaseAmount,
                null,
                ct);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var entity = PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(),
            d.ReceiptVoucherLineId,
            d.PaymentVoucherLineId,
            targetType,
            d.TargetDocumentId,
            source.CurrencyId,
            source.CurrencyCode,
            d.AllocatedAmount,
            source.ExchangeRate,
            sourceBaseAmount,
            now,
            validation.TargetBaseAllocatedAmount,
            targetDocumentAmount: validation.TargetDocumentAmount,
            idempotencyKey: idempotencyKey);
        await repository.AddAsync(entity, ct);

        if (targetType == DomainAllocationTargetDocumentType.PurchaseInvoice)
        {
            if (source.Context.SupplierId is not Guid supplierId || supplierId == Guid.Empty ||
                source.CounterpartyAccountId is not Guid supplierAccountId || supplierAccountId == Guid.Empty ||
                !source.PostingDate.HasValue)
            {
                throw new ConflictException(
                    "purchase_invoice_fx_source_incomplete",
                    "بيانات المورد أو حساب الذمم أو تاريخ السداد غير مكتملة لإنشاء قيد فرق الصرف.");
            }

            if (!Guid.TryParse(currentUser.UserId, out var userId))
                throw new ForbiddenException();

            await supplierSettlementFxPosting.PostAsync(
                new SupplierSettlementFxPostingRequest(
                    entity.Id,
                    supplierId,
                    supplierAccountId,
                    source.PostingDate.Value,
                    sourceBaseAmount,
                    validation.TargetBaseAllocatedAmount,
                    $"فرق صرف محقق لتخصيص دفعة على فاتورة مورد {d.TargetDocumentId}"),
                userId,
                now,
                ct);
        }

        return entity.Id;
    }

    private async Task<PaymentAllocation?> FindByIdempotencyKeyAsync(string key, CancellationToken ct)
    {
        var matches = await repository.ListAsync(
            new Specification<PaymentAllocation>()
                .Where(x => x.IdempotencyKey == key)
                .ApplyPaging(0, 2),
            ct);
        return matches.FirstOrDefault();
    }

    private static void EnsureReplayMatches(
        PaymentAllocation existing,
        OAS.Contracts.Accounting.PaymentAllocations.CreatePaymentAllocationRequest request,
        DomainAllocationTargetDocumentType targetType)
    {
        var sourceMatches =
            existing.ReceiptVoucherLineId == request.ReceiptVoucherLineId &&
            existing.PaymentVoucherLineId == request.PaymentVoucherLineId &&
            existing.CustomerAdvanceApplicationId == request.CustomerAdvanceApplicationId;

        if (!sourceMatches ||
            existing.TargetDocumentType != targetType ||
            existing.TargetDocumentId != request.TargetDocumentId ||
            existing.AllocatedAmount != request.AllocatedAmount)
        {
            throw new ConflictException(
                "payment_allocation_idempotency_conflict",
                "مفتاح منع التكرار مستخدم لعملية تخصيص مختلفة.");
        }
    }

    private static string? NormalizeIdempotencyKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > 80)
            throw new ConflictException("payment_allocation_idempotency_key_invalid", "مفتاح منع التكرار يجب ألا يتجاوز 80 حرفًا.");
        return normalized;
    }

    private async Task<SourceInfo> ResolveSourceAsync(Guid? receiptLineId, Guid? paymentLineId, CancellationToken ct)
    {
        if (receiptLineId is Guid rid)
        {
            var line = await receiptLines.GetByIdAsync(rid, ct) ?? throw new NotFoundException(nameof(ReceiptVoucherLine), rid);
            var voucher = await receiptVouchers.GetByIdAsync(line.ReceiptVoucherId, ct) ?? throw new NotFoundException(nameof(ReceiptVoucher), line.ReceiptVoucherId);
            if (voucher.Status != OAS.Domain.Accounting.Enums.ReceiptVoucherStatus.Posted)
                throw new ConflictException("payment_allocation_receipt_not_posted", "لا يمكن تخصيص دفعة من سند قبض غير مرحل.");
            return ToSource(
                line.CurrencyId,
                line.CurrencyCodeSnapshot,
                line.Amount,
                line.ExchangeRate,
                "receipt",
                new PaymentAllocationSourceContext(
                    OAS.Domain.Accounting.Enums.PaymentSourceType.ReceiptVoucher,
                    line.PartyType,
                    line.CustomerId,
                    line.SupplierId,
                    line.EmployeeId),
                line.CounterpartyAccountId,
                voucher.VoucherDate);
        }
        if (paymentLineId is Guid pid)
        {
            var line = await paymentLines.GetByIdAsync(pid, ct) ?? throw new NotFoundException(nameof(PaymentVoucherLine), pid);
            var voucher = await paymentVouchers.GetByIdAsync(line.PaymentVoucherId, ct) ?? throw new NotFoundException(nameof(PaymentVoucher), line.PaymentVoucherId);
            if (voucher.Status != OAS.Domain.Accounting.Enums.PaymentVoucherStatus.Posted)
                throw new ConflictException("payment_allocation_payment_not_posted", "لا يمكن تخصيص دفعة من سند صرف غير مرحل.");
            return ToSource(
                line.CurrencyId,
                line.CurrencyCodeSnapshot,
                line.Amount,
                line.ExchangeRate,
                "payment",
                new PaymentAllocationSourceContext(
                    OAS.Domain.Accounting.Enums.PaymentSourceType.PaymentVoucher,
                    line.PartyType,
                    line.CustomerId,
                    line.SupplierId,
                    line.EmployeeId),
                line.CounterpartyAccountId,
                voucher.VoucherDate);
        }
        throw new ConflictException("payment_source_line_required", "Exactly one receipt or payment voucher line must be selected.");
    }

    private static SourceInfo ToSource(
        Guid? id,
        string? code,
        decimal amount,
        decimal? rate,
        string type,
        PaymentAllocationSourceContext context,
        Guid? counterpartyAccountId,
        DateOnly postingDate)
    {
        if (id is not Guid currencyId || string.IsNullOrWhiteSpace(code) || rate is null or <= 0)
            throw new ConflictException("payment_source_line_not_migrated", $"The selected {type} voucher line does not contain multi-currency settlement data.");
        return new SourceInfo(currencyId, code, amount, rate.Value, context, counterpartyAccountId, postingDate);
    }

    private sealed record SourceInfo(
        Guid CurrencyId,
        string CurrencyCode,
        decimal Amount,
        decimal ExchangeRate,
        PaymentAllocationSourceContext Context,
        Guid? CounterpartyAccountId,
        DateOnly? PostingDate);
}
