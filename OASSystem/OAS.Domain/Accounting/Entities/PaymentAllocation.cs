using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class PaymentAllocation : AuditableEntity<Guid>
{
    private PaymentAllocation() { }

    public PaymentSourceType PaymentSourceType { get; private set; }
    public Guid PaymentSourceId { get; private set; }

    public Guid? ReceiptVoucherLineId { get; private set; }
    public Guid? PaymentVoucherLineId { get; private set; }
    public Guid? CustomerAdvanceApplicationId { get; private set; }

    public AllocationTargetDocumentType TargetDocumentType { get; private set; }
    public Guid TargetDocumentId { get; private set; }

    public Guid? CurrencyId { get; private set; }
    public string? CurrencyCodeSnapshot { get; private set; }
    public decimal AllocatedAmount { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public decimal? BaseAllocatedAmount { get; private set; }
    public decimal? TargetBaseAllocatedAmount { get; private set; }
    public decimal? TargetDocumentAmount { get; private set; }
    public DateTime AllocatedAtUtc { get; private set; }

    public string? IdempotencyKey { get; private set; }
    public DateTime? ReversedAtUtc { get; private set; }
    public string? ReversedBy { get; private set; }
    public string? ReversalReason { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public bool IsReversed => ReversedAtUtc.HasValue;

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
        DateTime allocatedAtUtc,
        decimal? targetBaseAllocatedAmount = null,
        Guid? customerAdvanceApplicationId = null,
        decimal? targetDocumentAmount = null,
        string? idempotencyKey = null)
    {
        ValidateCommon(id, targetDocumentId, allocatedAmount);

        if (receiptVoucherLineId == Guid.Empty)
            throw new ArgumentException("Receipt voucher line id cannot be empty.", nameof(receiptVoucherLineId));
        if (paymentVoucherLineId == Guid.Empty)
            throw new ArgumentException("Payment voucher line id cannot be empty.", nameof(paymentVoucherLineId));
        if (customerAdvanceApplicationId == Guid.Empty)
            throw new ArgumentException("Customer advance application id cannot be empty.", nameof(customerAdvanceApplicationId));

        var sourceCount =
            (receiptVoucherLineId.HasValue ? 1 : 0) +
            (paymentVoucherLineId.HasValue ? 1 : 0) +
            (customerAdvanceApplicationId.HasValue ? 1 : 0);

        if (sourceCount != 1)
            throw new InvalidOperationException("Exactly one payment source line/application must be selected.");

        if (currencyId == Guid.Empty)
            throw new ArgumentException("Currency is required.", nameof(currencyId));
        if (string.IsNullOrWhiteSpace(currencyCodeSnapshot))
            throw new ArgumentException("Currency code snapshot is required.", nameof(currencyCodeSnapshot));
        if (exchangeRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(exchangeRate));
        if (baseAllocatedAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(baseAllocatedAmount));
        if (targetBaseAllocatedAmount.HasValue && targetBaseAllocatedAmount.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetBaseAllocatedAmount));
        if (targetDocumentAmount.HasValue && targetDocumentAmount.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetDocumentAmount));

        var legacySourceType = receiptVoucherLineId.HasValue
            ? PaymentSourceType.ReceiptVoucher
            : paymentVoucherLineId.HasValue
                ? PaymentSourceType.PaymentVoucher
                : PaymentSourceType.CustomerAdvance;

        var legacySourceId =
            receiptVoucherLineId ??
            paymentVoucherLineId ??
            customerAdvanceApplicationId!.Value;

        return new PaymentAllocation
        {
            Id = id,
            PaymentSourceType = legacySourceType,
            PaymentSourceId = legacySourceId,
            ReceiptVoucherLineId = Normalize(receiptVoucherLineId),
            PaymentVoucherLineId = Normalize(paymentVoucherLineId),
            CustomerAdvanceApplicationId = Normalize(customerAdvanceApplicationId),
            TargetDocumentType = targetDocumentType,
            TargetDocumentId = targetDocumentId,
            CurrencyId = currencyId,
            CurrencyCodeSnapshot = currencyCodeSnapshot.Trim().ToUpperInvariant(),
            AllocatedAmount = allocatedAmount,
            ExchangeRate = exchangeRate,
            BaseAllocatedAmount = baseAllocatedAmount,
            TargetBaseAllocatedAmount =
                targetBaseAllocatedAmount ?? baseAllocatedAmount,
            TargetDocumentAmount = targetDocumentAmount,
            AllocatedAtUtc = allocatedAtUtc,
            IdempotencyKey = NormalizeIdempotencyKey(idempotencyKey)
        };
    }

    // Backward-compatible extension for SA-02:
    // existing callers can keep using 11 args; new callers may additionally persist
    // invoice-currency target amount and idempotency metadata.
    public static PaymentAllocation CreateCustomerAdvanceAllocation(
        Guid id,
        Guid customerAdvanceApplicationId,
        AllocationTargetDocumentType targetDocumentType,
        Guid targetDocumentId,
        Guid currencyId,
        string currencyCodeSnapshot,
        decimal allocatedAmount,
        decimal exchangeRate,
        decimal baseAllocatedAmount,
        decimal targetBaseAllocatedAmount,
        DateTime allocatedAtUtc,
        decimal? targetDocumentAmount = null,
        string? idempotencyKey = null) =>
        CreateLineAllocation(
            id,
            null,
            null,
            targetDocumentType,
            targetDocumentId,
            currencyId,
            currencyCodeSnapshot,
            allocatedAmount,
            exchangeRate,
            baseAllocatedAmount,
            allocatedAtUtc,
            targetBaseAllocatedAmount,
            customerAdvanceApplicationId,
            targetDocumentAmount,
            idempotencyKey);

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

        if (paymentSourceId == Guid.Empty)
            throw new ArgumentException("Payment source id is required.", nameof(paymentSourceId));

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

    public void UpdateAllocatedAmount(
        decimal allocatedAmount,
        decimal? baseAllocatedAmount = null,
        decimal? targetBaseAllocatedAmount = null,
        decimal? targetDocumentAmount = null)
    {
        if (IsReversed)
            throw new InvalidOperationException("A reversed payment allocation cannot be edited.");
        if (allocatedAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(allocatedAmount), "Allocated amount must be greater than zero.");
        if (baseAllocatedAmount.HasValue && baseAllocatedAmount.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(baseAllocatedAmount));
        if (targetBaseAllocatedAmount.HasValue && targetBaseAllocatedAmount.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetBaseAllocatedAmount));
        if (targetDocumentAmount.HasValue && targetDocumentAmount.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetDocumentAmount));

        AllocatedAmount = allocatedAmount;
        if (baseAllocatedAmount.HasValue)
            BaseAllocatedAmount = baseAllocatedAmount.Value;
        if (targetBaseAllocatedAmount.HasValue)
            TargetBaseAllocatedAmount = targetBaseAllocatedAmount.Value;
        if (targetDocumentAmount.HasValue)
            TargetDocumentAmount = targetDocumentAmount.Value;
    }

    public void Reverse(
        DateTime reversedAtUtc,
        string reversedBy,
        string reason)
    {
        if (IsReversed)
            throw new InvalidOperationException("Payment allocation has already been reversed.");
        if (string.IsNullOrWhiteSpace(reversedBy))
            throw new ArgumentException("Reversed by is required.", nameof(reversedBy));
        if (reversedBy.Trim().Length > 64)
            throw new ArgumentOutOfRangeException(nameof(reversedBy));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reversal reason is required.", nameof(reason));
        if (reason.Trim().Length > 300)
            throw new ArgumentOutOfRangeException(nameof(reason));

        ReversedAtUtc = reversedAtUtc;
        ReversedBy = reversedBy.Trim();
        ReversalReason = reason.Trim();
    }

    private static void ValidateCommon(
        Guid id,
        Guid targetDocumentId,
        decimal allocatedAmount)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));
        if (targetDocumentId == Guid.Empty)
            throw new ArgumentException("Target document id is required.", nameof(targetDocumentId));
        if (allocatedAmount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(allocatedAmount),
                "Allocated amount must be greater than zero.");
    }

    private static Guid? Normalize(Guid? value) =>
        value is { } id && id != Guid.Empty
            ? id
            : null;

    private static string? NormalizeIdempotencyKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        if (normalized.Length > 80)
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Idempotency key cannot exceed 80 characters.");

        return normalized;
    }
}
