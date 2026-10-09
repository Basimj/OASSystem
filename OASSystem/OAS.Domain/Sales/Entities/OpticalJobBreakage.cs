using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class OpticalJobBreakage : AuditableEntity<Guid>
{
    private OpticalJobBreakage() { }
    private OpticalJobBreakage(Guid id, Guid opticalJobId, Guid opticalJobLineId, Guid productVariantId, EyeSide? eye,
        decimal quantity, string reasonCode, string? reasonText, Guid? technicianId, bool requiresReplacement,
        Guid recordedBy, DateTimeOffset recordedAtUtc, string? idempotencyKey)
    {
        Id = SalesDomainGuard.Required(id, "Breakage id");
        OpticalJobId = SalesDomainGuard.Required(opticalJobId, "Optical job id");
        OpticalJobLineId = SalesDomainGuard.Required(opticalJobLineId, "Optical job line id");
        ProductVariantId = SalesDomainGuard.Required(productVariantId, "Product variant id");
        if (quantity <= 0m) throw new DomainException("Breakage quantity must be greater than zero.");
        if (eye.HasValue) SalesDomainGuard.Defined(eye.Value, "Eye side");
        if (technicianId == Guid.Empty) throw new DomainException("Technician id cannot be empty.");
        Quantity = decimal.Round(quantity, 3);
        Eye = eye;
        ReasonCode = SalesDomainGuard.Required(reasonCode, 50, "Breakage reason code");
        ReasonText = SalesDomainGuard.Optional(reasonText, 1000, "Breakage reason");
        TechnicianId = technicianId;
        RequiresReplacement = requiresReplacement;
        RecordedBy = SalesDomainGuard.Required(recordedBy, "Recorded by");
        RecordedAtUtc = recordedAtUtc;
        IdempotencyKey = SalesDomainGuard.Optional(idempotencyKey, 100, "Breakage idempotency key");
        Status = OpticalBreakageStatus.Recorded;
    }

    public Guid OpticalJobId { get; private set; }
    public Guid OpticalJobLineId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public EyeSide? Eye { get; private set; }
    public decimal Quantity { get; private set; }
    public string ReasonCode { get; private set; } = string.Empty;
    public string? ReasonText { get; private set; }
    public Guid? TechnicianId { get; private set; }
    public OpticalBreakageStatus Status { get; private set; }
    public bool RequiresReplacement { get; private set; }
    public Guid? InventoryTransactionId { get; private set; }
    public Guid? JournalEntryId { get; private set; }
    public Guid RecordedBy { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static OpticalJobBreakage Create(Guid id, Guid opticalJobId, Guid opticalJobLineId, Guid productVariantId,
        EyeSide? eye, decimal quantity, string reasonCode, string? reasonText, Guid? technicianId,
        bool requiresReplacement, Guid recordedBy, DateTimeOffset recordedAtUtc, string? idempotencyKey) =>
        new(id, opticalJobId, opticalJobLineId, productVariantId, eye, quantity, reasonCode, reasonText,
            technicianId, requiresReplacement, recordedBy, recordedAtUtc, idempotencyKey);

    public void LinkScrap(Guid transactionId, Guid? journalEntryId)
    {
        if (InventoryTransactionId.HasValue) throw new DomainException("Breakage scrap is already posted.");
        InventoryTransactionId = SalesDomainGuard.Required(transactionId, "Inventory transaction id");
        if (journalEntryId == Guid.Empty) throw new DomainException("Journal entry id cannot be empty.");
        JournalEntryId = journalEntryId;
    }

    public void MarkReplacementAvailable() => Status = OpticalBreakageStatus.ReplacementAvailable;
    public void MarkAwaitingReplacement() => Status = OpticalBreakageStatus.AwaitingReplacement;
    public void MarkReplacementReceived() => Status = OpticalBreakageStatus.ReplacementReceived;
    public void Close(DateTimeOffset atUtc) { Status = OpticalBreakageStatus.Closed; ClosedAtUtc = atUtc; }
    public void Cancel() { if (Status == OpticalBreakageStatus.Closed) throw new DomainException("Closed breakage cannot be cancelled."); Status = OpticalBreakageStatus.Cancelled; }
}
