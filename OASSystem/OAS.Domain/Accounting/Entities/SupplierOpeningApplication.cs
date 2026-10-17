using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class SupplierOpeningApplication : AuditableEntity<Guid>
{
    private SupplierOpeningApplication() { }

    private SupplierOpeningApplication(
        Guid id,
        Guid supplierOpeningItemId,
        Guid paymentVoucherLineId,
        Guid supplierId,
        decimal appliedSourceBaseAmount,
        decimal appliedTargetBaseAmount,
        DateTime appliedAtUtc,
        string appliedBy,
        string? idempotencyKey)
    {
        if (id == Guid.Empty) throw new ArgumentException("Opening application id is required.", nameof(id));
        if (supplierOpeningItemId == Guid.Empty) throw new ArgumentException("Opening item id is required.", nameof(supplierOpeningItemId));
        if (paymentVoucherLineId == Guid.Empty) throw new ArgumentException("Payment voucher line id is required.", nameof(paymentVoucherLineId));
        if (supplierId == Guid.Empty) throw new ArgumentException("Supplier id is required.", nameof(supplierId));
        if (appliedSourceBaseAmount <= 0m || appliedTargetBaseAmount <= 0m)
            throw new ArgumentOutOfRangeException(nameof(appliedSourceBaseAmount));
        if (string.IsNullOrWhiteSpace(appliedBy)) throw new ArgumentException("Applied by is required.", nameof(appliedBy));
        Id = id;
        SupplierOpeningItemId = supplierOpeningItemId;
        PaymentVoucherLineId = paymentVoucherLineId;
        SupplierId = supplierId;
        AppliedSourceBaseAmount = Round(appliedSourceBaseAmount);
        AppliedTargetBaseAmount = Round(appliedTargetBaseAmount);
        AppliedAtUtc = EnsureUtc(appliedAtUtc);
        AppliedBy = appliedBy.Trim();
        IdempotencyKey = Normalize(idempotencyKey, 80);
        Status = SupplierOpeningApplicationStatus.Active;
    }

    public Guid SupplierOpeningItemId { get; private set; }
    public Guid PaymentVoucherLineId { get; private set; }
    public Guid SupplierId { get; private set; }
    public decimal AppliedSourceBaseAmount { get; private set; }
    public decimal AppliedTargetBaseAmount { get; private set; }
    public DateTime AppliedAtUtc { get; private set; }
    public string AppliedBy { get; private set; } = string.Empty;
    public SupplierOpeningApplicationStatus Status { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTime? ReversedAtUtc { get; private set; }
    public string? ReversedBy { get; private set; }
    public string? ReversalReason { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static SupplierOpeningApplication Create(
        Guid id,
        Guid supplierOpeningItemId,
        Guid paymentVoucherLineId,
        Guid supplierId,
        decimal appliedSourceBaseAmount,
        decimal appliedTargetBaseAmount,
        DateTime appliedAtUtc,
        string appliedBy,
        string? idempotencyKey = null) =>
        new(id, supplierOpeningItemId, paymentVoucherLineId, supplierId,
            appliedSourceBaseAmount, appliedTargetBaseAmount, appliedAtUtc, appliedBy, idempotencyKey);

    public void Reverse(DateTime reversedAtUtc, string reversedBy, string reason)
    {
        if (Status != SupplierOpeningApplicationStatus.Active)
            throw new InvalidOperationException("Only an active opening application can be reversed.");
        if (string.IsNullOrWhiteSpace(reversedBy)) throw new ArgumentException("Reversed by is required.", nameof(reversedBy));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reversal reason is required.", nameof(reason));
        Status = SupplierOpeningApplicationStatus.Reversed;
        ReversedAtUtc = EnsureUtc(reversedAtUtc);
        ReversedBy = reversedBy.Trim();
        ReversalReason = Normalize(reason, 300);
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    private static decimal Round(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);
    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentOutOfRangeException(nameof(value));
        return normalized;
    }
}
