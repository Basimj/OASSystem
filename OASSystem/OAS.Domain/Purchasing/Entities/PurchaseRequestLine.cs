using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Rules;

namespace OAS.Domain.Purchasing.Entities;

public sealed class PurchaseRequestLine : AuditableEntity<Guid>
{
    private PurchaseRequestLine() { }
    private PurchaseRequestLine(Guid id, Guid purchaseRequestId, int lineSequence, Guid productVariantId, decimal requestedQuantity,
        DateOnly? requiredDate, Guid? customerOrderLineId, Guid? preferredSupplierId, string? notes)
    {
        Id = PurchasingDomainGuard.Required(id, "Purchase request line id");
        PurchaseRequestId = PurchasingDomainGuard.Required(purchaseRequestId, "Purchase request id");
        Update(lineSequence, productVariantId, requestedQuantity, requiredDate, customerOrderLineId, preferredSupplierId, notes);
    }
    public Guid PurchaseRequestId { get; private set; }
    public int LineSequence { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public decimal RequestedQuantity { get; private set; }
    public DateOnly? RequiredDate { get; private set; }
    public Guid? CustomerOrderLineId { get; private set; }
    public Guid? PreferredSupplierId { get; private set; }
    public DateTimeOffset? ScheduledOrderAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static PurchaseRequestLine Create(Guid id, Guid purchaseRequestId, int lineSequence, Guid productVariantId, decimal requestedQuantity,
        DateOnly? requiredDate = null, Guid? customerOrderLineId = null, Guid? preferredSupplierId = null, string? notes = null) =>
        new(id, purchaseRequestId, lineSequence, productVariantId, requestedQuantity, requiredDate, customerOrderLineId, preferredSupplierId, notes);

    public void ScheduleOrder(DateTimeOffset? scheduledOrderAtUtc)
    {
        ScheduledOrderAtUtc = scheduledOrderAtUtc;
    }

    public void Update(int lineSequence, Guid productVariantId, decimal requestedQuantity, DateOnly? requiredDate,
        Guid? customerOrderLineId, Guid? preferredSupplierId, string? notes)
    {
        if (lineSequence <= 0) throw new DomainException("Line sequence must be greater than zero.");
        PurchasingDomainGuard.Positive(requestedQuantity, "Requested quantity");
        if (customerOrderLineId == Guid.Empty) throw new DomainException("Customer order line id cannot be empty.");
        if (preferredSupplierId == Guid.Empty) throw new DomainException("Preferred supplier id cannot be empty.");
        LineSequence = lineSequence;
        ProductVariantId = PurchasingDomainGuard.Required(productVariantId, "Product variant id");
        RequestedQuantity = requestedQuantity;
        RequiredDate = requiredDate;
        CustomerOrderLineId = customerOrderLineId;
        PreferredSupplierId = preferredSupplierId;
        Notes = PurchasingDomainGuard.Optional(notes, 500, "Line notes");
    }
}
