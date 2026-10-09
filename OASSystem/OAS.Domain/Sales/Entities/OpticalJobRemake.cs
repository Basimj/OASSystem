using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class OpticalJobRemake : AuditableEntity<Guid>
{
    private OpticalJobRemake() { }
    private OpticalJobRemake(Guid id, Guid opticalJobId, Guid? sourceQualityCheckId, Guid? sourceBreakageId,
        Guid opticalJobLineId, Guid? productVariantId, decimal quantity, string reason, Guid createdBy, DateTimeOffset createdAtUtc)
    {
        Id = SalesDomainGuard.Required(id, "Remake id");
        OpticalJobId = SalesDomainGuard.Required(opticalJobId, "Optical job id");
        if (sourceQualityCheckId == Guid.Empty || sourceBreakageId == Guid.Empty || productVariantId == Guid.Empty)
            throw new DomainException("Optional remake identifiers cannot be empty.");
        OpticalJobLineId = SalesDomainGuard.Required(opticalJobLineId, "Optical job line id");
        if (quantity <= 0m) throw new DomainException("Remake quantity must be greater than zero.");
        SourceQualityCheckId = sourceQualityCheckId;
        SourceBreakageId = sourceBreakageId;
        ProductVariantId = productVariantId;
        Quantity = decimal.Round(quantity, 3);
        Reason = SalesDomainGuard.Required(reason, 1000, "Remake reason");
        CreatedByUserId = SalesDomainGuard.Required(createdBy, "Remake created by");
        SetCreatedAudit(createdAtUtc, createdBy.ToString());
        Status = OpticalRemakeStatus.Open;
    }

    public Guid OpticalJobId { get; private set; }
    public Guid? SourceQualityCheckId { get; private set; }
    public Guid? SourceBreakageId { get; private set; }
    public Guid OpticalJobLineId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public decimal Quantity { get; private set; }
    public OpticalRemakeStatus Status { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid? ReplacementPurchaseRequestLineId { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static OpticalJobRemake Create(Guid id, Guid opticalJobId, Guid? sourceQualityCheckId, Guid? sourceBreakageId,
        Guid opticalJobLineId, Guid? productVariantId, decimal quantity, string reason, Guid createdBy, DateTimeOffset createdAtUtc) =>
        new(id, opticalJobId, sourceQualityCheckId, sourceBreakageId, opticalJobLineId, productVariantId, quantity, reason, createdBy, createdAtUtc);

    public void MarkAwaitingMaterials(Guid? purchaseRequestLineId = null)
    {
        if (purchaseRequestLineId == Guid.Empty) throw new DomainException("Purchase request line id cannot be empty.");
        ReplacementPurchaseRequestLineId = purchaseRequestLineId;
        Status = OpticalRemakeStatus.AwaitingMaterials;
    }
    public void MarkReady() => Status = OpticalRemakeStatus.Ready;
    public void Start(DateTimeOffset atUtc) { if (Status is not (OpticalRemakeStatus.Open or OpticalRemakeStatus.Ready)) throw new DomainException("Remake is not ready to start."); Status = OpticalRemakeStatus.InProduction; StartedAtUtc ??= atUtc; }
    public void SendToQc() { if (Status != OpticalRemakeStatus.InProduction) throw new DomainException("Remake must be in production before QC."); Status = OpticalRemakeStatus.AwaitingQC; }
    public void Complete(DateTimeOffset atUtc) { if (Status != OpticalRemakeStatus.AwaitingQC) throw new DomainException("Remake must be awaiting QC before completion."); Status = OpticalRemakeStatus.Completed; CompletedAtUtc = atUtc; }
    public void Cancel() { if (Status == OpticalRemakeStatus.Completed) throw new DomainException("Completed remake cannot be cancelled."); Status = OpticalRemakeStatus.Cancelled; }
}
