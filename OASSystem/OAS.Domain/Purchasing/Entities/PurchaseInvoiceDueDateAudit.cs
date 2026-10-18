using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Purchasing.Rules;

namespace OAS.Domain.Purchasing.Entities;

public sealed class PurchaseInvoiceDueDateAudit : AuditableEntity<Guid>
{
    private PurchaseInvoiceDueDateAudit() { }

    private PurchaseInvoiceDueDateAudit(
        Guid id,
        Guid purchaseInvoiceId,
        DateOnly? oldDueDate,
        int? oldPaymentTermDays,
        PurchaseInvoiceDueDateProvenance oldProvenance,
        DateOnly newDueDate,
        int? newPaymentTermDays,
        string reason,
        string verifiedBy,
        DateTime verifiedAtUtc)
    {
        Id = PurchasingDomainGuard.Required(id, "Due-date audit id");
        PurchaseInvoiceId = PurchasingDomainGuard.Required(purchaseInvoiceId, "Purchase invoice id");
        if (oldPaymentTermDays < 0 || newPaymentTermDays < 0)
            throw new DomainException("Payment term days cannot be negative.");
        if (!Enum.IsDefined(oldProvenance))
            throw new DomainException("Previous due-date provenance is invalid.");
        OldDueDate = oldDueDate;
        OldPaymentTermDays = oldPaymentTermDays;
        OldProvenance = oldProvenance;
        NewDueDate = newDueDate;
        NewPaymentTermDays = newPaymentTermDays;
        Reason = PurchasingDomainGuard.Required(reason, 500, "Due-date verification reason");
        VerifiedBy = PurchasingDomainGuard.Required(verifiedBy, 64, "Verified by");
        VerifiedAtUtc = verifiedAtUtc.Kind == DateTimeKind.Utc ? verifiedAtUtc : verifiedAtUtc.ToUniversalTime();
    }

    public Guid PurchaseInvoiceId { get; private set; }
    public DateOnly? OldDueDate { get; private set; }
    public int? OldPaymentTermDays { get; private set; }
    public PurchaseInvoiceDueDateProvenance OldProvenance { get; private set; }
    public DateOnly NewDueDate { get; private set; }
    public int? NewPaymentTermDays { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string VerifiedBy { get; private set; } = string.Empty;
    public DateTime VerifiedAtUtc { get; private set; }

    public static PurchaseInvoiceDueDateAudit Create(
        Guid id,
        Guid purchaseInvoiceId,
        DateOnly? oldDueDate,
        int? oldPaymentTermDays,
        PurchaseInvoiceDueDateProvenance oldProvenance,
        DateOnly newDueDate,
        int? newPaymentTermDays,
        string reason,
        string verifiedBy,
        DateTime verifiedAtUtc) =>
        new(id, purchaseInvoiceId, oldDueDate, oldPaymentTermDays, oldProvenance,
            newDueDate, newPaymentTermDays, reason, verifiedBy, verifiedAtUtc);
}
