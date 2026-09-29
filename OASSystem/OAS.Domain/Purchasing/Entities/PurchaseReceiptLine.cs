using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Rules;

namespace OAS.Domain.Purchasing.Entities;

public sealed class PurchaseReceiptLine : AuditableEntity<Guid>
{
    private PurchaseReceiptLine() { }
    private PurchaseReceiptLine(Guid id, Guid purchaseReceiptId, Guid purchaseOrderLineId, int lineSequence, Guid productVariantId,
        decimal orderedQuantitySnapshot, decimal previouslyReceivedQty, decimal receivedQuantity, decimal acceptedQuantity,
        decimal rejectedQuantity, decimal unitConversionFactor, decimal actualUnitCost, DateOnly? expiryDate, string? batchCode, string? notes)
    {
        Id = PurchasingDomainGuard.Required(id, "Purchase receipt line id");
        PurchaseReceiptId = PurchasingDomainGuard.Required(purchaseReceiptId, "Purchase receipt id");
        PurchaseOrderLineId = PurchasingDomainGuard.Required(purchaseOrderLineId, "Purchase order line id");
        ProductVariantId = PurchasingDomainGuard.Required(productVariantId, "Product variant id");
        if(lineSequence<=0) throw new DomainException("Line sequence must be greater than zero.");
        PurchasingDomainGuard.Positive(orderedQuantitySnapshot,"Ordered quantity snapshot");
        PurchasingDomainGuard.NonNegative(previouslyReceivedQty,"Previously received quantity");
        PurchasingDomainGuard.Positive(unitConversionFactor,"Unit conversion factor");
        LineSequence=lineSequence; OrderedQuantitySnapshot=orderedQuantitySnapshot; PreviouslyReceivedQty=previouslyReceivedQty;
        SetReceiptQuantities(receivedQuantity,acceptedQuantity,rejectedQuantity,unitConversionFactor,actualUnitCost);
        ExpiryDate=expiryDate; BatchCode=PurchasingDomainGuard.Optional(batchCode,100,"Batch code"); Notes=PurchasingDomainGuard.Optional(notes,500,"Notes");
    }
    public Guid PurchaseReceiptId { get; private set; }
    public Guid PurchaseOrderLineId { get; private set; }
    public int LineSequence { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public decimal OrderedQuantitySnapshot { get; private set; }
    public decimal PreviouslyReceivedQty { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public decimal AcceptedQuantity { get; private set; }
    public decimal RejectedQuantity { get; private set; }
    public decimal BaseAcceptedQuantity { get; private set; }
    public decimal ActualUnitCost { get; private set; }
    public decimal TotalAcceptedCost { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public string? BatchCode { get; private set; }
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public decimal RemainingReceivableQuantity => OrderedQuantitySnapshot - PreviouslyReceivedQty;

    public static PurchaseReceiptLine Create(Guid id,Guid purchaseReceiptId,Guid purchaseOrderLineId,int lineSequence,Guid productVariantId,
        decimal orderedQuantitySnapshot,decimal previouslyReceivedQty,decimal receivedQuantity,decimal acceptedQuantity,decimal rejectedQuantity,
        decimal unitConversionFactor,decimal actualUnitCost,DateOnly? expiryDate,string? batchCode,string? notes)=>
        new(id,purchaseReceiptId,purchaseOrderLineId,lineSequence,productVariantId,orderedQuantitySnapshot,previouslyReceivedQty,receivedQuantity,
            acceptedQuantity,rejectedQuantity,unitConversionFactor,actualUnitCost,expiryDate,batchCode,notes);

    public void Update(decimal receivedQuantity,decimal acceptedQuantity,decimal rejectedQuantity,decimal unitConversionFactor,decimal actualUnitCost,
        DateOnly? expiryDate,string? batchCode,string? notes){SetReceiptQuantities(receivedQuantity,acceptedQuantity,rejectedQuantity,unitConversionFactor,actualUnitCost);ExpiryDate=expiryDate;BatchCode=PurchasingDomainGuard.Optional(batchCode,100,"Batch code");Notes=PurchasingDomainGuard.Optional(notes,500,"Notes");}

    private void SetReceiptQuantities(decimal receivedQuantity,decimal acceptedQuantity,decimal rejectedQuantity,decimal unitConversionFactor,decimal actualUnitCost)
    {
        PurchasingDomainGuard.Positive(receivedQuantity,"Received quantity"); PurchasingDomainGuard.NonNegative(acceptedQuantity,"Accepted quantity"); PurchasingDomainGuard.NonNegative(rejectedQuantity,"Rejected quantity");
        if(acceptedQuantity+rejectedQuantity!=receivedQuantity)throw new DomainException("Accepted quantity plus rejected quantity must equal received quantity.");
        if(acceptedQuantity>RemainingReceivableQuantity)throw new DomainException("Accepted quantity cannot exceed remaining receivable quantity.");
        PurchasingDomainGuard.Positive(unitConversionFactor,"Unit conversion factor"); PurchasingDomainGuard.NonNegative(actualUnitCost,"Actual unit cost");
        ReceivedQuantity=receivedQuantity;AcceptedQuantity=acceptedQuantity;RejectedQuantity=rejectedQuantity;BaseAcceptedQuantity=Math.Round(acceptedQuantity*unitConversionFactor,3);
        ActualUnitCost=actualUnitCost;TotalAcceptedCost=Math.Round(BaseAcceptedQuantity*actualUnitCost,4);
    }
}
