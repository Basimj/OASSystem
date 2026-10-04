using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Purchasing.Rules;

namespace OAS.Domain.Purchasing.Entities;

public sealed class PurchaseReturn : AuditableEntity<Guid>
{
    private readonly List<PurchaseReturnLine> _lines = [];
    private PurchaseReturn() { }

    private PurchaseReturn(Guid id, string returnCode, Guid purchaseReceiptId, Guid? purchaseInvoiceId,
        Guid supplierId, Guid warehouseId, DateOnly returnDate, DateOnly postingDate, string? reason)
    {
        Id = PurchasingDomainGuard.Required(id, "Purchase return id");
        ReturnCode = PurchasingDomainGuard.Required(returnCode, 40, "Purchase return code");
        PurchaseReceiptId = PurchasingDomainGuard.Required(purchaseReceiptId, "Purchase receipt id");
        if (purchaseInvoiceId == Guid.Empty) throw new DomainException("Purchase invoice id cannot be empty.");
        PurchaseInvoiceId = purchaseInvoiceId;
        SupplierId = PurchasingDomainGuard.Required(supplierId, "Supplier id");
        WarehouseId = PurchasingDomainGuard.Required(warehouseId, "Warehouse id");
        ReturnDate = returnDate;
        PostingDate = postingDate;
        Reason = PurchasingDomainGuard.Optional(reason, 1000, "Return reason");
        Status = PurchaseReturnStatus.Draft;
    }

    public string ReturnCode { get; private set; } = string.Empty;
    public Guid PurchaseReceiptId { get; private set; }
    public Guid? PurchaseInvoiceId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateOnly ReturnDate { get; private set; }
    public DateOnly PostingDate { get; private set; }
    public PurchaseReturnStatus Status { get; private set; }
    public decimal ReceiptCostBaseAmount { get; private set; }
    public decimal SupplierNetBaseAmount { get; private set; }
    public decimal SupplierTaxBaseAmount { get; private set; }
    public decimal SupplierGrossBaseAmount { get; private set; }
    public decimal InventoryCostBaseAmount { get; private set; }
    public decimal PurchasePriceVarianceBaseAmount { get; private set; }
    public string? Reason { get; private set; }
    public Guid? JournalEntryId { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public string? ConfirmedBy { get; private set; }
    public DateTimeOffset? PostedAt { get; private set; }
    public string? PostedBy { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<PurchaseReturnLine> Lines => _lines.AsReadOnly();

    public static PurchaseReturn Create(Guid id, string returnCode, Guid purchaseReceiptId, Guid? purchaseInvoiceId,
        Guid supplierId, Guid warehouseId, DateOnly returnDate, DateOnly postingDate, string? reason) =>
        new(id, returnCode, purchaseReceiptId, purchaseInvoiceId, supplierId, warehouseId, returnDate, postingDate, reason);

    public void AddLine(PurchaseReturnLine line)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(line);
        if (line.PurchaseReturnId != Id) throw new DomainException("Purchase return line does not belong to this return.");
        if (_lines.Any(x => x.Id == line.Id || x.LineNumber == line.LineNumber || x.PurchaseReceiptLineId == line.PurchaseReceiptLineId))
            throw new DomainException("Duplicate purchase return line is not allowed.");
        _lines.Add(line);
        RecalculateTotals();
    }

    public void Confirm(DateTimeOffset at, string? by)
    {
        EnsureDraft();
        if (_lines.Count == 0 || _lines.All(x => !x.IsActive)) throw new DomainException("Purchase return requires at least one line.");
        Status = PurchaseReturnStatus.Confirmed;
        ConfirmedAt = at;
        ConfirmedBy = PurchasingDomainGuard.User(by);
    }

    public void RefreshInventoryTotals()
    {
        if (Status != PurchaseReturnStatus.Confirmed) throw new DomainException("Inventory cost can be captured only for a confirmed purchase return.");
        if (_lines.Where(x => x.IsActive).Any(x => !x.InventoryCostBaseAmount.HasValue))
            throw new DomainException("Inventory cost snapshot is missing for one or more purchase return lines.");
        RecalculateTotals();
    }

    public void MarkPosted(Guid journalEntryId, DateTimeOffset at, string? by)
    {
        if (Status != PurchaseReturnStatus.Confirmed) throw new DomainException("Only confirmed purchase returns can be posted.");
        if (_lines.Where(x => x.IsActive).Any(x => !x.InventoryCostBaseAmount.HasValue))
            throw new DomainException("Inventory cost snapshot is required before posting purchase return.");
        JournalEntryId = PurchasingDomainGuard.Required(journalEntryId, "Journal entry id");
        Status = PurchaseReturnStatus.Posted;
        PostedAt = at;
        PostedBy = PurchasingDomainGuard.User(by);
    }

    public void Cancel(DateTimeOffset at, string? by, string? reason)
    {
        if (Status is not (PurchaseReturnStatus.Draft or PurchaseReturnStatus.Confirmed))
            throw new DomainException("Posted purchase returns cannot be cancelled directly.");
        foreach (var line in _lines) line.Deactivate();
        Status = PurchaseReturnStatus.Cancelled;
        CancelledAt = at;
        CancelledBy = PurchasingDomainGuard.User(by);
        CancellationReason = PurchasingDomainGuard.Optional(reason, 500, "Cancellation reason");
    }

    private void RecalculateTotals()
    {
        var active = _lines.Where(x => x.IsActive).ToArray();
        ReceiptCostBaseAmount = Math.Round(active.Sum(x => x.ReceiptCostBaseAmount), 4);
        SupplierNetBaseAmount = Math.Round(active.Sum(x => x.SupplierNetBaseAmount), 4);
        SupplierTaxBaseAmount = Math.Round(active.Sum(x => x.SupplierTaxBaseAmount), 4);
        SupplierGrossBaseAmount = Math.Round(active.Sum(x => x.SupplierGrossBaseAmount), 4);
        InventoryCostBaseAmount = Math.Round(active.Sum(x => x.InventoryCostBaseAmount ?? 0m), 4);
        PurchasePriceVarianceBaseAmount = PurchaseInvoiceId.HasValue
            ? Math.Round(SupplierNetBaseAmount - InventoryCostBaseAmount, 4)
            : Math.Round(ReceiptCostBaseAmount - InventoryCostBaseAmount, 4);
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseReturnStatus.Draft) throw new DomainException("Only draft purchase returns can be modified.");
    }
}
