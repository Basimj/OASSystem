using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Purchasing.Rules;

namespace OAS.Domain.Purchasing.Entities;

public sealed class PurchaseReceipt : AuditableEntity<Guid>
{
    private readonly List<PurchaseReceiptLine> _lines=[];
    private PurchaseReceipt() { }
    private PurchaseReceipt(Guid id,string receiptCode,Guid purchaseOrderId,Guid supplierId,Guid warehouseId,DateOnly receiptDate,DateOnly postingDate,string? supplierDeliveryCode,string? notes)
    {
        Id=PurchasingDomainGuard.Required(id,"Purchase receipt id");ReceiptCode=PurchasingDomainGuard.Required(receiptCode,40,"Receipt code");PurchaseOrderId=PurchasingDomainGuard.Required(purchaseOrderId,"Purchase order id");SupplierId=PurchasingDomainGuard.Required(supplierId,"Supplier id");WarehouseId=PurchasingDomainGuard.Required(warehouseId,"Warehouse id");ReceiptDate=receiptDate;PostingDate=postingDate;SupplierDeliveryCode=PurchasingDomainGuard.Optional(supplierDeliveryCode,100,"Supplier delivery code");Notes=PurchasingDomainGuard.Optional(notes,1000,"Notes");Status=PurchaseReceiptStatus.Draft;
    }
    public string ReceiptCode{get;private set;}=string.Empty; public Guid PurchaseOrderId{get;private set;} public Guid SupplierId{get;private set;} public Guid WarehouseId{get;private set;}
    public DateOnly ReceiptDate{get;private set;} public DateOnly PostingDate{get;private set;} public string? SupplierDeliveryCode{get;private set;} public PurchaseReceiptStatus Status{get;private set;}
    public Guid? InventoryTransactionId{get;private set;} public Guid? JournalEntryId{get;private set;} public string? Notes{get;private set;}
    public string? ConfirmedBy{get;private set;} public DateTimeOffset? ConfirmedAt{get;private set;} public string? PostedBy{get;private set;} public DateTimeOffset? PostedAt{get;private set;}
    public string? CancelledBy{get;private set;} public DateTimeOffset? CancelledAt{get;private set;} public string? CancellationReason{get;private set;} public byte[] RowVersion{get;private set;}=[];
    public IReadOnlyCollection<PurchaseReceiptLine> Lines=>_lines.AsReadOnly();
    public static PurchaseReceipt Create(Guid id,string receiptCode,Guid purchaseOrderId,Guid supplierId,Guid warehouseId,DateOnly receiptDate,DateOnly postingDate,string? supplierDeliveryCode,string? notes)=>new(id,receiptCode,purchaseOrderId,supplierId,warehouseId,receiptDate,postingDate,supplierDeliveryCode,notes);
    public void UpdateHeader(DateOnly receiptDate,DateOnly postingDate,string? supplierDeliveryCode,string? notes){EnsureDraft();ReceiptDate=receiptDate;PostingDate=postingDate;SupplierDeliveryCode=PurchasingDomainGuard.Optional(supplierDeliveryCode,100,"Supplier delivery code");Notes=PurchasingDomainGuard.Optional(notes,1000,"Notes");}
    public void AddLine(PurchaseReceiptLine line){EnsureDraft();ArgumentNullException.ThrowIfNull(line);if(line.PurchaseReceiptId!=Id)throw new DomainException("Purchase receipt line does not belong to this receipt.");if(_lines.Any(x=>x.Id==line.Id||x.LineSequence==line.LineSequence||x.PurchaseOrderLineId==line.PurchaseOrderLineId))throw new DomainException("Duplicate purchase receipt line is not allowed.");_lines.Add(line);}
    public void RemoveLine(Guid lineId){EnsureDraft();var line=_lines.SingleOrDefault(x=>x.Id==lineId)??throw new DomainException("Purchase receipt line was not found.");_lines.Remove(line);}
    public void Confirm(DateTimeOffset at,string? by){EnsureDraft();if(_lines.Count==0)throw new DomainException("Purchase receipt must contain at least one line before confirmation.");Status=PurchaseReceiptStatus.Confirmed;ConfirmedAt=at;ConfirmedBy=PurchasingDomainGuard.User(by);}
    public void MarkPosted(Guid inventoryTransactionId,Guid journalEntryId,DateTimeOffset at,string? by){if(Status!=PurchaseReceiptStatus.Confirmed)throw new DomainException("Only confirmed purchase receipts can be posted.");if(InventoryTransactionId.HasValue||JournalEntryId.HasValue)throw new DomainException("Purchase receipt is already linked to posting results.");InventoryTransactionId=PurchasingDomainGuard.Required(inventoryTransactionId,"Inventory transaction id");JournalEntryId=PurchasingDomainGuard.Required(journalEntryId,"Journal entry id");Status=PurchaseReceiptStatus.Posted;PostedAt=at;PostedBy=PurchasingDomainGuard.User(by);}
    public void Cancel(DateTimeOffset at,string? by,string? reason){if(Status is not (PurchaseReceiptStatus.Draft or PurchaseReceiptStatus.Confirmed))throw new DomainException("Posted purchase receipts cannot be cancelled directly.");Status=PurchaseReceiptStatus.Cancelled;CancelledAt=at;CancelledBy=PurchasingDomainGuard.User(by);CancellationReason=PurchasingDomainGuard.Optional(reason,500,"Cancellation reason");}
    private void EnsureDraft(){if(Status!=PurchaseReceiptStatus.Draft)throw new DomainException("Only draft purchase receipts can be modified.");}
}
