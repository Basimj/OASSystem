using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Purchasing.Rules;
using OAS.Domain.Sales.Enums;

namespace OAS.Domain.Purchasing.Entities;

public sealed class PurchaseOrder : AuditableEntity<Guid>
{
    private readonly List<PurchaseOrderLine> _lines = [];
    private PurchaseOrder() { }

    private PurchaseOrder(
        Guid id,
        string code,
        Guid supplierId,
        Guid destinationWarehouseId,
        DateOnly orderDate,
        DateOnly? expectedDeliveryDate,
        Guid currencyId,
        decimal exchangeRate,
        DateOnly exchangeRateDate,
        TaxCalculationMode taxCalculationMode,
        int paymentTermDays,
        string? notes)
    {
        Id = PurchasingDomainGuard.Required(id, "Purchase order id");
        PurchaseOrderCode = PurchasingDomainGuard.Required(code, 40, "Purchase order code");
        Status = PurchaseOrderStatus.Draft;
        UpdateHeader(supplierId, destinationWarehouseId, orderDate, expectedDeliveryDate, currencyId, exchangeRate,
            exchangeRateDate, taxCalculationMode, paymentTermDays, notes);
    }

    public string PurchaseOrderCode { get; private set; } = string.Empty;
    public Guid SupplierId { get; private set; }
    public Guid DestinationWarehouseId { get; private set; }
    public DateOnly OrderDate { get; private set; }
    public DateOnly? ExpectedDeliveryDate { get; private set; }
    public Guid CurrencyId { get; private set; }
    public decimal ExchangeRate { get; private set; }
    public DateOnly ExchangeRateDate { get; private set; }
    public TaxCalculationMode TaxCalculationMode { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public int PaymentTermDays { get; private set; }
    public string? Notes { get; private set; }
    public string? SubmittedBy { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public string? RejectedBy { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? SentBy { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public string? ClosedBy { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public string? CancelledBy { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    public static PurchaseOrder Create(
        Guid id,
        string code,
        Guid supplierId,
        Guid destinationWarehouseId,
        DateOnly orderDate,
        DateOnly? expectedDeliveryDate,
        Guid currencyId,
        decimal exchangeRate,
        DateOnly exchangeRateDate,
        TaxCalculationMode taxCalculationMode,
        int paymentTermDays,
        string? notes) =>
        new(id, code, supplierId, destinationWarehouseId, orderDate, expectedDeliveryDate, currencyId,
            exchangeRate, exchangeRateDate, taxCalculationMode, paymentTermDays, notes);

    public void UpdateHeader(
        Guid supplierId,
        Guid destinationWarehouseId,
        DateOnly orderDate,
        DateOnly? expectedDeliveryDate,
        Guid currencyId,
        decimal exchangeRate,
        DateOnly exchangeRateDate,
        TaxCalculationMode taxCalculationMode,
        int paymentTermDays,
        string? notes)
    {
        EnsureDraft();
        SupplierId = PurchasingDomainGuard.Required(supplierId, "Supplier id");
        DestinationWarehouseId = PurchasingDomainGuard.Required(destinationWarehouseId, "Destination warehouse id");
        CurrencyId = PurchasingDomainGuard.Required(currencyId, "Currency id");
        PurchasingDomainGuard.Positive(exchangeRate, "Exchange rate");
        if (!Enum.IsDefined(taxCalculationMode)) throw new DomainException("Tax calculation mode is invalid.");
        if (paymentTermDays < 0) throw new DomainException("Payment term days cannot be negative.");
        if (expectedDeliveryDate.HasValue && expectedDeliveryDate.Value < orderDate)
            throw new DomainException("Expected delivery date cannot be before order date.");
        OrderDate = orderDate;
        ExpectedDeliveryDate = expectedDeliveryDate;
        ExchangeRate = exchangeRate;
        ExchangeRateDate = exchangeRateDate;
        TaxCalculationMode = taxCalculationMode;
        PaymentTermDays = paymentTermDays;
        Notes = PurchasingDomainGuard.Optional(notes, 1000, "Notes");
    }

    public void AddLine(PurchaseOrderLine line)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(line);
        if (line.PurchaseOrderId != Id) throw new DomainException("Purchase order line does not belong to this order.");
        if (_lines.Any(x => x.Id == line.Id || x.LineSequence == line.LineSequence))
            throw new DomainException("Duplicate purchase order line sequence is not allowed.");
        _lines.Add(line);
        RecalculateTotals();
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();
        var line = _lines.SingleOrDefault(x => x.Id == lineId) ?? throw new DomainException("Purchase order line was not found.");
        _lines.Remove(line);
        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        Subtotal = Math.Round(_lines.Sum(x => x.OrderedQuantity * x.UnitPrice), 4);
        DiscountAmount = Math.Round(_lines.Sum(x => x.DiscountAmount), 4);
        TaxAmount = Math.Round(_lines.Sum(x => x.TaxAmount), 4);
        TotalAmount = Math.Round(_lines.Sum(x => x.FinalAmount), 4);
    }

    public void Submit(DateTimeOffset at, string? by)
    {
        EnsureDraft();
        if (_lines.Count == 0) throw new DomainException("Purchase order must contain at least one line before submission.");
        RecalculateTotals();
        Status = PurchaseOrderStatus.PendingApproval;
        SubmittedAt = at;
        SubmittedBy = PurchasingDomainGuard.User(by);
    }

    public void Approve(DateTimeOffset at, string? by)
    {
        if (Status != PurchaseOrderStatus.PendingApproval) throw new DomainException("Only pending purchase orders can be approved.");
        Status = PurchaseOrderStatus.Approved;
        ApprovedAt = at;
        ApprovedBy = PurchasingDomainGuard.User(by);
    }

    public void Reject(DateTimeOffset at, string? by, string reason)
    {
        if (Status != PurchaseOrderStatus.PendingApproval) throw new DomainException("Only pending purchase orders can be rejected.");
        Status = PurchaseOrderStatus.Rejected;
        RejectedAt = at;
        RejectedBy = PurchasingDomainGuard.User(by);
        RejectionReason = PurchasingDomainGuard.Required(reason, 500, "Rejection reason");
    }

    public void Send(DateTimeOffset at, string? by)
    {
        if (Status != PurchaseOrderStatus.Approved) throw new DomainException("Only approved purchase orders can be sent to supplier.");
        Status = PurchaseOrderStatus.Sent;
        SentAt = at;
        SentBy = PurchasingDomainGuard.User(by);
    }

    public void MarkReceived(bool fullyReceived)
    {
        if (Status is not (PurchaseOrderStatus.Sent or PurchaseOrderStatus.PartiallyReceived))
            throw new DomainException("Purchase order cannot be marked received in the current state.");
        Status = fullyReceived ? PurchaseOrderStatus.FullyReceived : PurchaseOrderStatus.PartiallyReceived;
    }

    public void Close(DateTimeOffset at, string? by)
    {
        if (Status != PurchaseOrderStatus.FullyReceived)
            throw new DomainException("Only fully received purchase orders can be closed after all closing checks pass.");
        Status = PurchaseOrderStatus.Closed;
        ClosedAt = at;
        ClosedBy = PurchasingDomainGuard.User(by);
    }

    public void Cancel(DateTimeOffset at, string? by, string? reason, bool hasPostedReceipt = false)
    {
        if (hasPostedReceipt) throw new DomainException("Purchase order with a posted receipt cannot be cancelled directly.");
        if (Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Approved or PurchaseOrderStatus.Sent))
            throw new DomainException("Purchase order cannot be cancelled in the current state.");
        Status = PurchaseOrderStatus.Cancelled;
        CancelledAt = at;
        CancelledBy = PurchasingDomainGuard.User(by);
        CancellationReason = PurchasingDomainGuard.Optional(reason, 500, "Cancellation reason");
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseOrderStatus.Draft) throw new DomainException("Only draft purchase orders can be modified.");
    }
}
