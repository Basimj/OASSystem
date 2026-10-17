using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Purchasing.Rules;

namespace OAS.Domain.Purchasing.Entities;

/// <summary>
/// Open-item settlement that applies the AP credit already posted by a purchase return
/// to a supplier invoice. Creating/reversing an application never creates another AP journal;
/// the purchase-return journal is the financial source of the credit.
/// </summary>
public sealed class SupplierCreditApplication : AuditableEntity<Guid>
{
    private SupplierCreditApplication() { }

    private SupplierCreditApplication(
        Guid id,
        Guid purchaseReturnId,
        Guid purchaseInvoiceId,
        Guid supplierId,
        decimal appliedSourceBaseAmount,
        decimal appliedTargetBaseAmount,
        decimal appliedInvoiceCurrencyAmount,
        DateTime appliedAtUtc,
        string appliedBy,
        string? idempotencyKey)
    {
        Id = PurchasingDomainGuard.Required(id, "Supplier credit application id");
        PurchaseReturnId = PurchasingDomainGuard.Required(purchaseReturnId, "Purchase return id");
        PurchaseInvoiceId = PurchasingDomainGuard.Required(purchaseInvoiceId, "Purchase invoice id");
        SupplierId = PurchasingDomainGuard.Required(supplierId, "Supplier id");
        AppliedSourceBaseAmount = Positive(appliedSourceBaseAmount, nameof(appliedSourceBaseAmount));
        AppliedTargetBaseAmount = Positive(appliedTargetBaseAmount, nameof(appliedTargetBaseAmount));
        AppliedInvoiceCurrencyAmount = Positive(appliedInvoiceCurrencyAmount, nameof(appliedInvoiceCurrencyAmount));
        AppliedAtUtc = RequireUtc(appliedAtUtc, nameof(appliedAtUtc));
        AppliedBy = PurchasingDomainGuard.Required(appliedBy, 64, "Applied by");
        IdempotencyKey = PurchasingDomainGuard.Optional(idempotencyKey, 80, "Idempotency key");
        Status = SupplierCreditApplicationStatus.Active;
    }

    public Guid PurchaseReturnId { get; private set; }
    public Guid PurchaseInvoiceId { get; private set; }
    public Guid SupplierId { get; private set; }
    public decimal AppliedSourceBaseAmount { get; private set; }
    public decimal AppliedTargetBaseAmount { get; private set; }
    public decimal AppliedInvoiceCurrencyAmount { get; private set; }
    public DateTime AppliedAtUtc { get; private set; }
    public string AppliedBy { get; private set; } = string.Empty;
    public SupplierCreditApplicationStatus Status { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTime? ReversedAtUtc { get; private set; }
    public string? ReversedBy { get; private set; }
    public string? ReversalReason { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static SupplierCreditApplication Create(
        Guid id,
        Guid purchaseReturnId,
        Guid purchaseInvoiceId,
        Guid supplierId,
        decimal appliedSourceBaseAmount,
        decimal appliedTargetBaseAmount,
        decimal appliedInvoiceCurrencyAmount,
        DateTime appliedAtUtc,
        string appliedBy,
        string? idempotencyKey = null) =>
        new(
            id,
            purchaseReturnId,
            purchaseInvoiceId,
            supplierId,
            appliedSourceBaseAmount,
            appliedTargetBaseAmount,
            appliedInvoiceCurrencyAmount,
            appliedAtUtc,
            appliedBy,
            idempotencyKey);

    public void Reverse(DateTime reversedAtUtc, string reversedBy, string reason)
    {
        if (Status != SupplierCreditApplicationStatus.Active)
            throw new DomainException("Only an active supplier credit application can be reversed.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Supplier credit application reversal reason is required.");

        ReversedAtUtc = RequireUtc(reversedAtUtc, nameof(reversedAtUtc));
        ReversedBy = PurchasingDomainGuard.Required(reversedBy, 64, "Reversed by");
        ReversalReason = PurchasingDomainGuard.Required(reason, 300, "Reversal reason");
        Status = SupplierCreditApplicationStatus.Reversed;
    }

    private static decimal Positive(decimal value, string name)
    {
        if (value <= 0m) throw new DomainException($"{name} must be greater than zero.");
        return Math.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    private static DateTime RequireUtc(DateTime value, string name)
    {
        if (value == default) throw new DomainException($"{name} is required.");
        return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    }
}
