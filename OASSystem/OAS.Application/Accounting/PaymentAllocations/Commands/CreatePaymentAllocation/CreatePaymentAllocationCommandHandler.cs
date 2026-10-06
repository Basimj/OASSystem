using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
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
    TimeProvider timeProvider) : IRequestHandler<CreatePaymentAllocationCommand, Guid>
{
    public async Task<Guid> Handle(CreatePaymentAllocationCommand request, CancellationToken ct)
    {
        var d = request.Data;
        var source = await ResolveSourceAsync(d.ReceiptVoucherLineId, d.PaymentVoucherLineId, ct);
        var existing = await repository.ListAsync(new Specification<PaymentAllocation>().Where(x =>
            d.ReceiptVoucherLineId.HasValue
                ? x.ReceiptVoucherLineId == d.ReceiptVoucherLineId
                : x.PaymentVoucherLineId == d.PaymentVoucherLineId), ct);
        var available = source.Amount - existing.Sum(x => x.AllocatedAmount);
        if (d.AllocatedAmount > available)
            throw new ConflictException("payment_allocation_exceeds_available_amount", $"The requested allocation ({d.AllocatedAmount}) exceeds the available source-line amount ({available}).");

        var sourceBaseAmount = Math.Round(d.AllocatedAmount * source.ExchangeRate, 4, MidpointRounding.AwayFromZero);
        var targetType = (DomainAllocationTargetDocumentType)(byte)d.TargetDocumentType;
        if (targetType is DomainAllocationTargetDocumentType.EmployeePayroll or DomainAllocationTargetDocumentType.EndOfServiceSettlement)
            throw new ConflictException("managed_payment_allocation_required", "Payroll and end-of-service allocations can only be created by their dedicated payment workflows.");
        var sourceTargetValidator = sourceTargetValidators.SingleOrDefault(x => x.TargetDocumentType == targetType);
        if (sourceTargetValidator is not null)
            await sourceTargetValidator.ValidateSourceAsync(d.TargetDocumentId, source.Context, ct);

        var validator = targetValidators.SingleOrDefault(x => x.TargetDocumentType == targetType);
        var targetBaseAmount = sourceBaseAmount;
        if (validator is not null)
        {
            targetBaseAmount = (await validator.ValidateAsync(
                d.TargetDocumentId,
                source.CurrencyId,
                d.AllocatedAmount,
                sourceBaseAmount,
                null,
                ct)).TargetBaseAllocatedAmount;
        }

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
            timeProvider.GetUtcNow().UtcDateTime,
            targetBaseAmount);
        await repository.AddAsync(entity, ct);
        return entity.Id;
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
                    line.EmployeeId));
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
                    line.EmployeeId));
        }
        throw new ConflictException("payment_source_line_required", "Exactly one receipt or payment voucher line must be selected.");
    }

    private static SourceInfo ToSource(
        Guid? id,
        string? code,
        decimal amount,
        decimal? rate,
        string type,
        PaymentAllocationSourceContext context)
    {
        if (id is not Guid currencyId || string.IsNullOrWhiteSpace(code) || rate is null or <= 0)
            throw new ConflictException("payment_source_line_not_migrated", $"The selected {type} voucher line does not contain multi-currency settlement data.");
        return new SourceInfo(currencyId, code, amount, rate.Value, context);
    }

    private sealed record SourceInfo(
        Guid CurrencyId,
        string CurrencyCode,
        decimal Amount,
        decimal ExchangeRate,
        PaymentAllocationSourceContext Context);
}
