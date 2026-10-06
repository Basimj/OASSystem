using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Purchasing.Rules;

namespace OAS.Domain.Purchasing.Entities;

public sealed class PurchaseRequest : AuditableEntity<Guid>
{
    private readonly List<PurchaseRequestLine> _lines = [];
    private PurchaseRequest() { }
    private PurchaseRequest(Guid id, string requestCode, PurchaseRequestType requestType, Guid warehouseId, Guid? customerOrderId,
        DateOnly requestDate, DateOnly? requiredDate, string? reason, string? notes, string? requestedBy)
    {
        Id = PurchasingDomainGuard.Required(id, "Purchase request id");
        RequestCode = PurchasingDomainGuard.Required(requestCode, 40, "Request code");
        PurchasingDomainGuard.Defined(requestType, "Purchase request type");
        RequestType = requestType;
        Status = PurchaseRequestStatus.Draft;
        UpdateHeader(warehouseId, customerOrderId, requestDate, requiredDate, reason, notes);
        RequestedBy = PurchasingDomainGuard.User(requestedBy);
    }
    public string RequestCode { get; private set; } = string.Empty;
    public PurchaseRequestType RequestType { get; private set; }
    public PurchaseRequestStatus Status { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? CustomerOrderId { get; private set; }
    public DateOnly RequestDate { get; private set; }
    public DateOnly? RequiredDate { get; private set; }
    public string? Reason { get; private set; }
    public string? Notes { get; private set; }
    public string? RequestedBy { get; private set; }
    public string? SubmittedBy { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public string? RejectedBy { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? CancelledBy { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<PurchaseRequestLine> Lines => _lines.AsReadOnly();

    public static PurchaseRequest Create(Guid id, string requestCode, PurchaseRequestType requestType, Guid warehouseId, Guid? customerOrderId,
        DateOnly requestDate, DateOnly? requiredDate, string? reason, string? notes, string? requestedBy) =>
        new(id, requestCode, requestType, warehouseId, customerOrderId, requestDate, requiredDate, reason, notes, requestedBy);

    public void UpdateRequestType(PurchaseRequestType requestType)
    {
        EnsureDraft();
        PurchasingDomainGuard.Defined(requestType, "Purchase request type");
        RequestType = requestType;
    }

    public void UpdateHeader(Guid warehouseId, Guid? customerOrderId, DateOnly requestDate, DateOnly? requiredDate, string? reason, string? notes)
    {
        EnsureDraft();
        WarehouseId = PurchasingDomainGuard.Required(warehouseId, "Warehouse id");
        if (requiredDate.HasValue && requiredDate.Value < requestDate) throw new DomainException("Required date cannot be before request date.");
        if (customerOrderId == Guid.Empty) throw new DomainException("Customer order id cannot be empty.");
        if (RequestType == PurchaseRequestType.CustomerDemand && !customerOrderId.HasValue) throw new DomainException("Customer demand requests require a customer order reference.");
        CustomerOrderId = customerOrderId;
        RequestDate = requestDate;
        RequiredDate = requiredDate;
        Reason = PurchasingDomainGuard.Optional(reason, 500, "Reason");
        Notes = PurchasingDomainGuard.Optional(notes, 1000, "Notes");
    }
    public void AddLine(PurchaseRequestLine line){EnsureDraft(); ArgumentNullException.ThrowIfNull(line); if(line.PurchaseRequestId!=Id)throw new DomainException("Purchase request line does not belong to this request."); if(_lines.Any(x=>x.Id==line.Id||x.LineSequence==line.LineSequence))throw new DomainException("Duplicate purchase request line sequence is not allowed."); _lines.Add(line);}
    public void RemoveLine(Guid lineId){EnsureDraft(); var line=_lines.SingleOrDefault(x=>x.Id==lineId)??throw new DomainException("Purchase request line was not found."); _lines.Remove(line);}
    public void Submit(DateTimeOffset at,string? by){EnsureDraft();if(_lines.Count==0)throw new DomainException("Purchase request must contain at least one line before submission.");Status=PurchaseRequestStatus.PendingApproval;SubmittedAt=at;SubmittedBy=PurchasingDomainGuard.User(by);}
    public void Approve(DateTimeOffset at,string? by){if(Status!=PurchaseRequestStatus.PendingApproval)throw new DomainException("Only pending purchase requests can be approved.");Status=PurchaseRequestStatus.Approved;ApprovedAt=at;ApprovedBy=PurchasingDomainGuard.User(by);}
    public void Reject(DateTimeOffset at,string? by,string reason){if(Status!=PurchaseRequestStatus.PendingApproval)throw new DomainException("Only pending purchase requests can be rejected.");Status=PurchaseRequestStatus.Rejected;RejectedAt=at;RejectedBy=PurchasingDomainGuard.User(by);RejectionReason=PurchasingDomainGuard.Required(reason,500,"Rejection reason");}
    public void MarkConversion(decimal allocatedQuantity, decimal requestedQuantityTotal){if(Status is not (PurchaseRequestStatus.Approved or PurchaseRequestStatus.PartiallyConverted or PurchaseRequestStatus.Converted))throw new DomainException("Only approved purchase requests can be converted.");PurchasingDomainGuard.NonNegative(allocatedQuantity,"Allocated quantity");PurchasingDomainGuard.Positive(requestedQuantityTotal,"Requested quantity total");if(allocatedQuantity>requestedQuantityTotal)throw new DomainException("Allocated quantity cannot exceed requested quantity.");Status=allocatedQuantity==requestedQuantityTotal?PurchaseRequestStatus.Converted:allocatedQuantity>0?PurchaseRequestStatus.PartiallyConverted:PurchaseRequestStatus.Approved;}
    public void Cancel(DateTimeOffset at,string? by,string? reason)
    {
        if(Status is not (PurchaseRequestStatus.Draft or PurchaseRequestStatus.Approved or PurchaseRequestStatus.PartiallyConverted))
            throw new DomainException("Purchase request cannot be cancelled in the current state.");
        Status=PurchaseRequestStatus.Cancelled;
        CancelledAt=at;
        CancelledBy=PurchasingDomainGuard.User(by);
        CancellationReason=PurchasingDomainGuard.Optional(reason,500,"Cancellation reason");
    }
    private void EnsureDraft(){if(Status!=PurchaseRequestStatus.Draft)throw new DomainException("Only draft purchase requests can be modified.");}
}
