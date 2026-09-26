using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainAllocationTargetDocumentType = OAS.Domain.Accounting.Enums.AllocationTargetDocumentType;
namespace OAS.Application.Accounting.PaymentAllocations.Commands.CreatePaymentAllocation;
public sealed class CreatePaymentAllocationCommandHandler(
    IRepository<PaymentAllocation, Guid> repository,
    IReadRepository<ReceiptVoucherLine, Guid> receiptLines,
    IReadRepository<PaymentVoucherLine, Guid> paymentLines,
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
        var baseAmount = Math.Round(d.AllocatedAmount * source.ExchangeRate, 4, MidpointRounding.AwayFromZero);
        var entity = PaymentAllocation.CreateLineAllocation(Guid.NewGuid(), d.ReceiptVoucherLineId, d.PaymentVoucherLineId,
            (DomainAllocationTargetDocumentType)(byte)d.TargetDocumentType, d.TargetDocumentId,
            source.CurrencyId, source.CurrencyCode, d.AllocatedAmount, source.ExchangeRate, baseAmount,
            timeProvider.GetUtcNow().UtcDateTime);
        await repository.AddAsync(entity, ct);
        return entity.Id;
    }

    private async Task<SourceInfo> ResolveSourceAsync(Guid? receiptLineId, Guid? paymentLineId, CancellationToken ct)
    {
        if (receiptLineId is Guid rid)
        {
            var line = await receiptLines.GetByIdAsync(rid, ct) ?? throw new NotFoundException(nameof(ReceiptVoucherLine), rid);
            return ToSource(line.CurrencyId, line.CurrencyCodeSnapshot, line.Amount, line.ExchangeRate, "receipt");
        }
        if (paymentLineId is Guid pid)
        {
            var line = await paymentLines.GetByIdAsync(pid, ct) ?? throw new NotFoundException(nameof(PaymentVoucherLine), pid);
            return ToSource(line.CurrencyId, line.CurrencyCodeSnapshot, line.Amount, line.ExchangeRate, "payment");
        }
        throw new ConflictException("payment_source_line_required", "Exactly one receipt or payment voucher line must be selected.");
    }
    private static SourceInfo ToSource(Guid? id, string? code, decimal amount, decimal? rate, string type)
    {
        if (id is not Guid currencyId || string.IsNullOrWhiteSpace(code) || rate is null or <= 0)
            throw new ConflictException("payment_source_line_not_migrated", $"The selected {type} voucher line does not contain multi-currency settlement data.");
        return new SourceInfo(currencyId, code, amount, rate.Value);
    }
    private sealed record SourceInfo(Guid CurrencyId, string CurrencyCode, decimal Amount, decimal ExchangeRate);
}
