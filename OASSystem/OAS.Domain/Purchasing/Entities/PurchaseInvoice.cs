using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Purchasing.Rules;
using OAS.Domain.Sales.Enums;

namespace OAS.Domain.Purchasing.Entities;

public sealed class PurchaseInvoice : AuditableEntity<Guid>
{
    private readonly List<PurchaseInvoiceLine> _lines = [];
    private PurchaseInvoice() { }

    private PurchaseInvoice(
        Guid id,
        string purchaseInvoiceCode,
        string? supplierInvoiceCode,
        Guid supplierId,
        DateOnly invoiceDate,
        DateOnly postingDate,
        Guid currencyId,
        decimal exchangeRate,
        DateOnly exchangeRateDate,
        TaxCalculationMode taxCalculationMode,
        string? notes)
    {
        Id = PurchasingDomainGuard.Required(id, "Purchase invoice id");
        PurchaseInvoiceCode = PurchasingDomainGuard.Required(purchaseInvoiceCode, 40, "Purchase invoice code");
        Status = PurchaseInvoiceStatus.Draft;
        UpdateHeader(supplierInvoiceCode, supplierId, invoiceDate, postingDate, currencyId, exchangeRate,
            exchangeRateDate, taxCalculationMode, notes);
    }

    public string PurchaseInvoiceCode { get; private set; } = string.Empty;
    public string? SupplierInvoiceCode { get; private set; }
    public Guid SupplierId { get; private set; }
    public DateOnly InvoiceDate { get; private set; }
    public DateOnly PostingDate { get; private set; }
    public Guid CurrencyId { get; private set; }
    public decimal ExchangeRate { get; private set; }
    public DateOnly ExchangeRateDate { get; private set; }
    public TaxCalculationMode TaxCalculationMode { get; private set; }
    public PurchaseInvoiceStatus Status { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal BaseSubtotal { get; private set; }
    public decimal BaseTaxAmount { get; private set; }
    public decimal BaseTotalAmount { get; private set; }
    public Guid? JournalEntryId { get; private set; }
    public string? Notes { get; private set; }
    public string? ConfirmedBy { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public string? PostedBy { get; private set; }
    public DateTimeOffset? PostedAt { get; private set; }
    public string? CancelledBy { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<PurchaseInvoiceLine> Lines => _lines.AsReadOnly();

    public static PurchaseInvoice Create(
        Guid id,
        string purchaseInvoiceCode,
        string? supplierInvoiceCode,
        Guid supplierId,
        DateOnly invoiceDate,
        DateOnly postingDate,
        Guid currencyId,
        decimal exchangeRate,
        DateOnly exchangeRateDate,
        TaxCalculationMode taxCalculationMode,
        string? notes) =>
        new(id, purchaseInvoiceCode, supplierInvoiceCode, supplierId, invoiceDate, postingDate, currencyId,
            exchangeRate, exchangeRateDate, taxCalculationMode, notes);

    public void UpdateHeader(
        string? supplierInvoiceCode,
        Guid supplierId,
        DateOnly invoiceDate,
        DateOnly postingDate,
        Guid currencyId,
        decimal exchangeRate,
        DateOnly exchangeRateDate,
        TaxCalculationMode taxCalculationMode,
        string? notes)
    {
        EnsureDraft();
        SupplierInvoiceCode = PurchasingDomainGuard.Optional(supplierInvoiceCode, 100, "Supplier invoice code");
        SupplierId = PurchasingDomainGuard.Required(supplierId, "Supplier id");
        CurrencyId = PurchasingDomainGuard.Required(currencyId, "Currency id");
        PurchasingDomainGuard.Positive(exchangeRate, "Exchange rate");
        if (!Enum.IsDefined(taxCalculationMode)) throw new DomainException("Tax calculation mode is invalid.");
        InvoiceDate = invoiceDate;
        PostingDate = postingDate;
        ExchangeRate = exchangeRate;
        ExchangeRateDate = exchangeRateDate;
        TaxCalculationMode = taxCalculationMode;
        Notes = PurchasingDomainGuard.Optional(notes, 1000, "Notes");
    }

    public void AddLine(PurchaseInvoiceLine line)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(line);
        if (line.PurchaseInvoiceId != Id) throw new DomainException("Purchase invoice line does not belong to this invoice.");
        if (_lines.Any(x => x.Id == line.Id || x.LineSequence == line.LineSequence))
            throw new DomainException("Duplicate purchase invoice line sequence is not allowed.");
        _lines.Add(line);
        RecalculateTotals();
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();
        var line = _lines.SingleOrDefault(x => x.Id == lineId) ?? throw new DomainException("Purchase invoice line was not found.");
        _lines.Remove(line);
        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        Subtotal = Math.Round(_lines.Sum(x => x.GrossAmount), 4);
        DiscountAmount = Math.Round(_lines.Sum(x => x.DiscountAmount), 4);
        TaxAmount = Math.Round(_lines.Sum(x => x.TaxAmount), 4);
        TotalAmount = Math.Round(_lines.Sum(x => x.FinalAmount), 4);
        BaseSubtotal = Math.Round(_lines.Sum(x => x.BaseNetAmount), 4);
        BaseTaxAmount = Math.Round(_lines.Sum(x => x.BaseTaxAmount), 4);
        BaseTotalAmount = Math.Round(_lines.Sum(x => x.BaseFinalAmount), 4);
    }

    public void Confirm(DateTimeOffset at, string? by)
    {
        EnsureDraft();
        if (_lines.Count == 0) throw new DomainException("Purchase invoice must contain at least one line before confirmation.");
        RecalculateTotals();
        Status = PurchaseInvoiceStatus.Confirmed;
        ConfirmedAt = at;
        ConfirmedBy = PurchasingDomainGuard.User(by);
    }

    public void MarkMatchApprovalRequired()
    {
        if (Status != PurchaseInvoiceStatus.Confirmed)
            throw new DomainException("Only confirmed purchase invoices can require match approval.");
        Status = PurchaseInvoiceStatus.PendingMatchApproval;
    }

    public void MarkMatchApproved()
    {
        if (Status != PurchaseInvoiceStatus.PendingMatchApproval)
            throw new DomainException("Purchase invoice is not pending match approval.");
        Status = PurchaseInvoiceStatus.Confirmed;
    }

    public void MarkPosted(Guid journalEntryId, DateTimeOffset at, string? by)
    {
        if (Status != PurchaseInvoiceStatus.Confirmed)
            throw new DomainException("Only confirmed and matched purchase invoices can be posted.");
        if (JournalEntryId.HasValue) throw new DomainException("Purchase invoice is already linked to a journal entry.");
        JournalEntryId = PurchasingDomainGuard.Required(journalEntryId, "Journal entry id");
        Status = PurchaseInvoiceStatus.Posted;
        PostedAt = at;
        PostedBy = PurchasingDomainGuard.User(by);
    }

    public void Cancel(DateTimeOffset at, string? by, string? reason)
    {
        if (Status is not (PurchaseInvoiceStatus.Draft or PurchaseInvoiceStatus.Confirmed or PurchaseInvoiceStatus.PendingMatchApproval))
            throw new DomainException("Posted purchase invoices cannot be cancelled directly.");
        Status = PurchaseInvoiceStatus.Cancelled;
        CancelledAt = at;
        CancelledBy = PurchasingDomainGuard.User(by);
        CancellationReason = PurchasingDomainGuard.Optional(reason, 500, "Cancellation reason");
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseInvoiceStatus.Draft) throw new DomainException("Only draft purchase invoices can be modified.");
    }
}
