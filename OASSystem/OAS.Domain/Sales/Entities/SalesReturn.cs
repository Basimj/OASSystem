using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class SalesReturn : AuditableEntity<Guid>
{
    private readonly List<SalesReturnLine> _lines = [];
    private SalesReturn() { }

    private SalesReturn(
        Guid id,
        string returnCode,
        Guid salesInvoiceId,
        Guid customerId,
        DateOnly returnDate,
        DateOnly postingDate,
        Guid currencyId,
        string currencyCodeSnapshot,
        byte currencyDecimalPlacesSnapshot,
        decimal exchangeRate,
        DateOnly exchangeRateDate,
        ExchangeRateType exchangeRateType,
        ExchangeRateSource exchangeRateSource,
        Guid baseCurrencyId,
        string baseCurrencyCodeSnapshot,
        byte baseCurrencyDecimalPlacesSnapshot,
        string? reason)
    {
        Id = SalesDomainGuard.Required(id, "Sales return id");
        ReturnCode = SalesDomainGuard.Required(returnCode, 40, "Sales return code");
        SalesInvoiceId = SalesDomainGuard.Required(salesInvoiceId, "Sales invoice id");
        CustomerId = SalesDomainGuard.Required(customerId, "Customer id");
        CurrencyId = SalesDomainGuard.Required(currencyId, "Currency id");
        BaseCurrencyId = SalesDomainGuard.Required(baseCurrencyId, "Base currency id");
        if (exchangeRate <= 0m) throw new DomainException("Exchange rate must be greater than zero.");
        if (currencyDecimalPlacesSnapshot > 6 || baseCurrencyDecimalPlacesSnapshot > 6)
            throw new DomainException("Currency decimal places cannot exceed 6.");

        ReturnDate = returnDate;
        PostingDate = postingDate;
        CurrencyCodeSnapshot = SalesDomainGuard.Required(currencyCodeSnapshot, 10, "Currency code snapshot");
        CurrencyDecimalPlacesSnapshot = currencyDecimalPlacesSnapshot;
        ExchangeRate = exchangeRate;
        ExchangeRateDate = exchangeRateDate;
        ExchangeRateType = exchangeRateType;
        ExchangeRateSource = exchangeRateSource;
        BaseCurrencyCodeSnapshot = SalesDomainGuard.Required(baseCurrencyCodeSnapshot, 10, "Base currency code snapshot");
        BaseCurrencyDecimalPlacesSnapshot = baseCurrencyDecimalPlacesSnapshot;
        Reason = SalesDomainGuard.Optional(reason, 1000, "Return reason");
        Status = SalesReturnStatus.Draft;
    }

    public string ReturnCode { get; private set; } = string.Empty;
    public Guid SalesInvoiceId { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateOnly ReturnDate { get; private set; }
    public DateOnly PostingDate { get; private set; }
    public SalesReturnStatus Status { get; private set; }
    public Guid CurrencyId { get; private set; }
    public string CurrencyCodeSnapshot { get; private set; } = string.Empty;
    public byte CurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal ExchangeRate { get; private set; }
    public DateOnly ExchangeRateDate { get; private set; }
    public ExchangeRateType ExchangeRateType { get; private set; }
    public ExchangeRateSource ExchangeRateSource { get; private set; }
    public Guid BaseCurrencyId { get; private set; }
    public string BaseCurrencyCodeSnapshot { get; private set; } = string.Empty;
    public byte BaseCurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal BaseNetAmount { get; private set; }
    public decimal BaseTaxAmount { get; private set; }
    public decimal BaseTotalAmount { get; private set; }
    public string? Reason { get; private set; }
    public Guid? JournalEntryId { get; private set; }
    public DateTimeOffset? ConfirmedAtUtc { get; private set; }
    public string? ConfirmedBy { get; private set; }
    public DateTimeOffset? PostedAtUtc { get; private set; }
    public string? PostedBy { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<SalesReturnLine> Lines => _lines.AsReadOnly();

    public static SalesReturn Create(
        Guid id, string returnCode, Guid salesInvoiceId, Guid customerId, DateOnly returnDate, DateOnly postingDate,
        Guid currencyId, string currencyCodeSnapshot, byte currencyDecimalPlacesSnapshot, decimal exchangeRate,
        DateOnly exchangeRateDate, ExchangeRateType exchangeRateType, ExchangeRateSource exchangeRateSource,
        Guid baseCurrencyId, string baseCurrencyCodeSnapshot, byte baseCurrencyDecimalPlacesSnapshot, string? reason) =>
        new(id, returnCode, salesInvoiceId, customerId, returnDate, postingDate, currencyId, currencyCodeSnapshot,
            currencyDecimalPlacesSnapshot, exchangeRate, exchangeRateDate, exchangeRateType, exchangeRateSource,
            baseCurrencyId, baseCurrencyCodeSnapshot, baseCurrencyDecimalPlacesSnapshot, reason);

    public void AddLine(SalesReturnLine line)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(line);
        if (line.SalesReturnId != Id) throw new DomainException("Sales return line does not belong to this return.");
        if (_lines.Any(x => x.Id == line.Id || x.LineNumber == line.LineNumber || x.SalesInvoiceLineId == line.SalesInvoiceLineId))
            throw new DomainException("Duplicate sales return line is not allowed.");
        _lines.Add(line);
        RecalculateTotals();
    }

    public void Confirm(DateTimeOffset atUtc, string? by)
    {
        EnsureDraft();
        if (_lines.Count == 0 || _lines.All(x => !x.IsActive))
            throw new DomainException("Sales return must contain at least one active line before confirmation.");
        Status = SalesReturnStatus.Confirmed;
        ConfirmedAtUtc = atUtc;
        ConfirmedBy = NormalizeUser(by);
    }

    public void MarkPosted(Guid journalEntryId, DateTimeOffset atUtc, string? by)
    {
        if (Status != SalesReturnStatus.Confirmed)
            throw new DomainException("Only confirmed sales returns can be posted.");
        JournalEntryId = SalesDomainGuard.Required(journalEntryId, "Journal entry id");
        Status = SalesReturnStatus.Posted;
        PostedAtUtc = atUtc;
        PostedBy = NormalizeUser(by);
    }

    public void Cancel(DateTimeOffset atUtc, string? by, string? reason)
    {
        if (Status is not (SalesReturnStatus.Draft or SalesReturnStatus.Confirmed))
            throw new DomainException("Posted sales returns cannot be cancelled directly.");
        foreach (var line in _lines) line.Deactivate();
        Status = SalesReturnStatus.Cancelled;
        CancelledAtUtc = atUtc;
        CancelledBy = NormalizeUser(by);
        CancellationReason = SalesDomainGuard.Optional(reason, 500, "Cancellation reason");
    }

    private void RecalculateTotals()
    {
        var active = _lines.Where(x => x.IsActive).ToArray();
        NetAmount = Math.Round(active.Sum(x => x.NetAmount), CurrencyDecimalPlacesSnapshot);
        TaxAmount = Math.Round(active.Sum(x => x.TaxAmount), CurrencyDecimalPlacesSnapshot);
        TotalAmount = Math.Round(NetAmount + TaxAmount, CurrencyDecimalPlacesSnapshot);
        BaseNetAmount = Math.Round(active.Sum(x => x.BaseNetAmount), BaseCurrencyDecimalPlacesSnapshot);
        BaseTaxAmount = Math.Round(active.Sum(x => x.BaseTaxAmount), BaseCurrencyDecimalPlacesSnapshot);
        BaseTotalAmount = Math.Round(BaseNetAmount + BaseTaxAmount, BaseCurrencyDecimalPlacesSnapshot);
    }

    private void EnsureDraft()
    {
        if (Status != SalesReturnStatus.Draft)
            throw new DomainException("Only draft sales returns can be modified.");
    }

    private static string? NormalizeUser(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length <= 64 ? value.Trim() : value.Trim()[..64];
}
