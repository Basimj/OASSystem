using OAS.Domain.Common.Entities;
using OAS.Domain.Purchasing.Rules;

namespace OAS.Domain.Purchasing.Entities;

public sealed class PurchaseOrderLineSource : AuditableEntity<Guid>
{
    private PurchaseOrderLineSource() { }
    private PurchaseOrderLineSource(Guid id, Guid purchaseOrderLineId, Guid purchaseRequestLineId, decimal allocatedQuantity)
    {
        Id = PurchasingDomainGuard.Required(id, "Purchase order line source id");
        PurchaseOrderLineId = PurchasingDomainGuard.Required(purchaseOrderLineId, "Purchase order line id");
        PurchaseRequestLineId = PurchasingDomainGuard.Required(purchaseRequestLineId, "Purchase request line id");
        PurchasingDomainGuard.Positive(allocatedQuantity, "Allocated quantity");
        AllocatedQuantity = allocatedQuantity;
    }
    public Guid PurchaseOrderLineId { get; private set; }
    public Guid PurchaseRequestLineId { get; private set; }
    public decimal AllocatedQuantity { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static PurchaseOrderLineSource Create(Guid id, Guid purchaseOrderLineId, Guid purchaseRequestLineId, decimal allocatedQuantity) =>
        new(id, purchaseOrderLineId, purchaseRequestLineId, allocatedQuantity);
}
