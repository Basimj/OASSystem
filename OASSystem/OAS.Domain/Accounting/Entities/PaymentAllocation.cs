using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class PaymentAllocation : AuditableEntity<Guid>
{
    private PaymentAllocation() { }

    // Legacy source pointer retained during Expand.
    public PaymentSourceType PaymentSourceType { get; private set; }
    public Guid PaymentSourceId { get; private set; }

    // Typed source-line foreign keys for new allocations. Exactly one must be set.
    public Guid? ReceiptVoucherLineId { get; private set; }
    public Guid? PaymentVoucherLineId { get; private set; }

    public AllocationTargetDocumentType TargetDocumentType { get; private set; }
    public Guid TargetDocumentId { get; private set; }

    public Guid? CurrencyId { get; private set; }
    public string? CurrencyCodeSnapshot { get; private set; }
    public decimal AllocatedAmount { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public decimal? BaseAllocatedAmount { get; private set; }
    public DateTime AllocatedAtUtc { get; private set; }

    public static PaymentAllocation CreateLineAllocation(
        Guid id,
        Guid? receiptVoucherLineId,
        Guid? paymentVoucherLineId,
        AllocationTargetDocumentType targetDocumentType,
        Guid targetDocumentId,
        Guid currencyId,
        string currencyCodeSnapshot,
        decimal allocatedAmount,
        decimal exchangeRate,
        decimal baseAllocatedAmount,
        DateTime allocatedAtUtc)
    {
        ValidateCommon(id, targetDocumentId, allocatedAmount);
        var sourceCount = (receiptVoucherLineId.HasValue ? 1 : 0) + (paymentVoucherLineId.HasValue ? 1 : 0);
        if (sourceCount != 1) throw new InvalidOperationException("Exactly one payment source line must be selected.");
        if (currencyId == Guid.Empty) throw new ArgumentException("Currency is required.", nameof(currencyId));
        if (string.IsNullOrWhiteSpace(currencyCodeSnapshot)) throw new ArgumentException("Currency code snapshot is required.", nameof(currencyCodeSnapshot));
        if (exchangeRate <= 0) throw new ArgumentOutOfRangeException(nameof(exchangeRate));
        if (baseAllocatedAmount <= 0) throw new ArgumentOutOfRangeException(nameof(baseAllocatedAmount));

        var legacySourceType = receiptVoucherLineId.HasValue ? PaymentSourceType.ReceiptVoucher : PaymentSourceType.PaymentVoucher;
        var legacySourceId = receiptVoucherLineId ?? paymentVoucherLineId!.Value;

        return new PaymentAllocation
        {
            Id = id,
            PaymentSourceType = legacySourceType,
            PaymentSourceId = legacySourceId,
            ReceiptVoucherLineId = Normalize(receiptVoucherLineId),
            PaymentVoucherLineId = Normalize(paymentVoucherLineId),
            TargetDocumentType = targetDocumentType,
            TargetDocumentId = targetDocumentId,
            CurrencyId = currencyId,
            CurrencyCodeSnapshot = currencyCodeSnapshot.Trim().ToUpperInvariant(),
            AllocatedAmount = allocatedAmount,
            ExchangeRate = exchangeRate,
            BaseAllocatedAmount = baseAllocatedAmount,
            AllocatedAtUtc = allocatedAtUtc
        };
    }

    public static PaymentAllocation Create(
        Guid id,
        PaymentSourceType paymentSourceType,
        Guid paymentSourceId,
        AllocationTargetDocumentType targetDocumentType,
        Guid targetDocumentId,
        decimal allocatedAmount,
        DateTime allocatedAtUtc)
    {
        ValidateCommon(id, targetDocumentId, allocatedAmount);
        if (paymentSourceId == Guid.Empty) throw new ArgumentException("Payment source id is required.", nameof(paymentSourceId));
        return new PaymentAllocation
        {
            Id = id,
            PaymentSourceType = paymentSourceType,
            PaymentSourceId = paymentSourceId,
            TargetDocumentType = targetDocumentType,
            TargetDocumentId = targetDocumentId,
            AllocatedAmount = allocatedAmount,
            AllocatedAtUtc = allocatedAtUtc
        };
    }

    public void UpdateAllocatedAmount(decimal allocatedAmount, decimal? baseAllocatedAmount = null)
    {
        if (allocatedAmount <= 0) throw new ArgumentOutOfRangeException(nameof(allocatedAmount), "Allocated amount must be greater than zero.");
        if (baseAllocatedAmount.HasValue && baseAllocatedAmount.Value <= 0) throw new ArgumentOutOfRangeException(nameof(baseAllocatedAmount));
        AllocatedAmount = allocatedAmount;
        if (baseAllocatedAmount.HasValue) BaseAllocatedAmount = baseAllocatedAmount.Value;
    }

    private static void ValidateCommon(Guid id, Guid targetDocumentId, decimal allocatedAmount)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (targetDocumentId == Guid.Empty) throw new ArgumentException("Target document id is required.", nameof(targetDocumentId));
        if (allocatedAmount <= 0) throw new ArgumentOutOfRangeException(nameof(allocatedAmount), "Allocated amount must be greater than zero.");
    }

    private static Guid? Normalize(Guid? value) => value is { } id && id != Guid.Empty ? id : null;
}
