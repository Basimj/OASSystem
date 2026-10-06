using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class SalesInvoice : AuditableEntity<Guid>
{
    private readonly List<SalesInvoiceLine> _lines = [];

    private SalesInvoice() { }

    private SalesInvoice(
        Guid id, string invoiceCode, Guid customerId, Guid? customerOrderId, Guid? prescriptionRevisionId,
        DateOnly invoiceDate, DateOnly postingDate, Guid currencyId, string currencyCodeSnapshot,
        string? currencySymbolSnapshot, byte currencyDecimalPlacesSnapshot, decimal exchangeRate,
        DateOnly exchangeRateDate, ExchangeRateType exchangeRateType, ExchangeRateSource exchangeRateSource,
        TaxCalculationMode taxCalculationMode, SalesPaymentPlan paymentPlan, int paymentTermDaysSnapshot,
        Guid baseCurrencyId, string baseCurrencyCodeSnapshot, byte baseCurrencyDecimalPlacesSnapshot, string? description)
    {
        Id = SalesDomainGuard.Required(id, "Sales invoice id");
        InvoiceCode = SalesDomainGuard.Required(invoiceCode, 40, "Invoice code");
        CustomerId = SalesDomainGuard.Required(customerId, "Customer id");
        ValidateOptionalId(customerOrderId, "Customer order id");
        ValidateOptionalId(prescriptionRevisionId, "Prescription revision id");
        CustomerOrderId = customerOrderId;
        PrescriptionRevisionId = prescriptionRevisionId;
        InvoiceDate = invoiceDate;
        PostingDate = postingDate;
        SetCurrencySnapshots(currencyId, currencyCodeSnapshot, currencySymbolSnapshot, currencyDecimalPlacesSnapshot,
            exchangeRate, exchangeRateDate, exchangeRateType, exchangeRateSource, baseCurrencyId,
            baseCurrencyCodeSnapshot, baseCurrencyDecimalPlacesSnapshot);
        SetCommercialPolicy(taxCalculationMode, paymentPlan, paymentTermDaysSnapshot);
        Description = SalesDomainGuard.Optional(description, 1000, "Invoice description");
        Status = SalesInvoiceStatus.Draft;
        IsActive = true;
    }

    public string InvoiceCode { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Guid? CustomerOrderId { get; private set; }
    public Guid? PrescriptionRevisionId { get; private set; }
    public Guid? SalesEmployeeId { get; private set; }
    public DateOnly InvoiceDate { get; private set; }
    public DateOnly PostingDate { get; private set; }
    public SalesInvoiceStatus Status { get; private set; }
    public Guid CurrencyId { get; private set; }
    public string CurrencyCodeSnapshot { get; private set; } = string.Empty;
    public string? CurrencySymbolSnapshot { get; private set; }
    public byte CurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal ExchangeRate { get; private set; }
    public DateOnly ExchangeRateDate { get; private set; }
    public ExchangeRateType ExchangeRateType { get; private set; }
    public ExchangeRateSource ExchangeRateSource { get; private set; }
    public TaxCalculationMode TaxCalculationMode { get; private set; }
    public SalesPaymentPlan PaymentPlan { get; private set; } = SalesPaymentPlan.FullNow;
    public SalesPaymentTermType PaymentTermType { get; private set; }
    public int PaymentTermDaysSnapshot { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public Guid BaseCurrencyId { get; private set; }
    public string BaseCurrencyCodeSnapshot { get; private set; } = string.Empty;
    public byte BaseCurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal BaseSubtotal { get; private set; }
    public decimal BaseDiscountAmount { get; private set; }
    public decimal BaseTaxAmount { get; private set; }
    public decimal BaseTotalAmount { get; private set; }
    public string? Description { get; private set; }
    public Guid? JournalEntryId { get; private set; }
    public DateTimeOffset? ConfirmedAtUtc { get; private set; }
    public string? ConfirmedBy { get; private set; }
    public DateTimeOffset? PostedAtUtc { get; private set; }
    public string? PostedBy { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancelledBy { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<SalesInvoiceLine> Lines => _lines.AsReadOnly();

    public static SalesInvoice Create(
        Guid id, string invoiceCode, Guid customerId, Guid? customerOrderId, Guid? prescriptionRevisionId,
        DateOnly invoiceDate, DateOnly postingDate, Guid currencyId, string currencyCodeSnapshot,
        string? currencySymbolSnapshot, byte currencyDecimalPlacesSnapshot, decimal exchangeRate,
        DateOnly exchangeRateDate, ExchangeRateType exchangeRateType, ExchangeRateSource exchangeRateSource,
        TaxCalculationMode taxCalculationMode, SalesPaymentPlan paymentPlan, int paymentTermDaysSnapshot,
        Guid baseCurrencyId, string baseCurrencyCodeSnapshot, byte baseCurrencyDecimalPlacesSnapshot,
        string? description = null) =>
        new(id, invoiceCode, customerId, customerOrderId, prescriptionRevisionId, invoiceDate, postingDate, currencyId,
            currencyCodeSnapshot, currencySymbolSnapshot, currencyDecimalPlacesSnapshot, exchangeRate, exchangeRateDate,
            exchangeRateType, exchangeRateSource, taxCalculationMode, paymentPlan, paymentTermDaysSnapshot,
            baseCurrencyId, baseCurrencyCodeSnapshot, baseCurrencyDecimalPlacesSnapshot, description);

    // Transitional overload for the current Application layer. Credit maps to AccountCredit and
    // Immediate maps to FullNow, matching the required data backfill rule.
    public static SalesInvoice Create(
        Guid id, string invoiceCode, Guid customerId, Guid? customerOrderId, Guid? prescriptionRevisionId,
        DateOnly invoiceDate, DateOnly postingDate, Guid currencyId, string currencyCodeSnapshot,
        string? currencySymbolSnapshot, byte currencyDecimalPlacesSnapshot, decimal exchangeRate,
        DateOnly exchangeRateDate, ExchangeRateType exchangeRateType, ExchangeRateSource exchangeRateSource,
        TaxCalculationMode taxCalculationMode, SalesPaymentTermType paymentTermType, int paymentTermDaysSnapshot,
        Guid baseCurrencyId, string baseCurrencyCodeSnapshot, byte baseCurrencyDecimalPlacesSnapshot,
        string? description = null) =>
        Create(id, invoiceCode, customerId, customerOrderId, prescriptionRevisionId, invoiceDate, postingDate,
            currencyId, currencyCodeSnapshot, currencySymbolSnapshot, currencyDecimalPlacesSnapshot, exchangeRate,
            exchangeRateDate, exchangeRateType, exchangeRateSource, taxCalculationMode,
            SalesPaymentPlanPolicy.FromLegacyPaymentTerm(paymentTermType), paymentTermDaysSnapshot,
            baseCurrencyId, baseCurrencyCodeSnapshot, baseCurrencyDecimalPlacesSnapshot, description);

    public void UpdateHeader(
        Guid customerId, Guid? customerOrderId, Guid? prescriptionRevisionId, DateOnly invoiceDate, DateOnly postingDate,
        TaxCalculationMode taxCalculationMode, SalesPaymentPlan paymentPlan, int paymentTermDaysSnapshot,
        string? description)
    {
        EnsureDraft();
        CustomerId = SalesDomainGuard.Required(customerId, "Customer id");
        ValidateOptionalId(customerOrderId, "Customer order id");
        ValidateOptionalId(prescriptionRevisionId, "Prescription revision id");
        CustomerOrderId = customerOrderId;
        PrescriptionRevisionId = prescriptionRevisionId;
        InvoiceDate = invoiceDate;
        PostingDate = postingDate;
        SetCommercialPolicy(taxCalculationMode, paymentPlan, paymentTermDaysSnapshot);
        Description = SalesDomainGuard.Optional(description, 1000, "Invoice description");
        RepriceLines();
    }

    // Transitional overload for callers still using SalesPaymentTermType.
    public void UpdateHeader(
        Guid customerId, Guid? customerOrderId, Guid? prescriptionRevisionId, DateOnly invoiceDate, DateOnly postingDate,
        TaxCalculationMode taxCalculationMode, SalesPaymentTermType paymentTermType, int paymentTermDaysSnapshot,
        string? description) =>
        UpdateHeader(customerId, customerOrderId, prescriptionRevisionId, invoiceDate, postingDate,
            taxCalculationMode, SalesPaymentPlanPolicy.FromLegacyPaymentTerm(paymentTermType),
            paymentTermDaysSnapshot, description);

    public void ChangePaymentPlanSnapshot(SalesPaymentPlan paymentPlan, int paymentTermDaysSnapshot = 0)
    {
        EnsureDraft();
        SetCommercialPolicy(TaxCalculationMode, paymentPlan, paymentTermDaysSnapshot);
    }

    public void SetSalesEmployee(Guid? employeeId)
    {
        EnsureDraft();
        ValidateOptionalId(employeeId, "Sales employee id");
        SalesEmployeeId = employeeId;
    }

    public void ChangeCurrency(
        Guid currencyId, string currencyCodeSnapshot, string? currencySymbolSnapshot, byte currencyDecimalPlacesSnapshot,
        decimal exchangeRate, DateOnly exchangeRateDate, ExchangeRateType exchangeRateType, ExchangeRateSource exchangeRateSource,
        Guid baseCurrencyId, string baseCurrencyCodeSnapshot, byte baseCurrencyDecimalPlacesSnapshot)
    {
        EnsureDraft();
        SetCurrencySnapshots(currencyId, currencyCodeSnapshot, currencySymbolSnapshot, currencyDecimalPlacesSnapshot,
            exchangeRate, exchangeRateDate, exchangeRateType, exchangeRateSource, baseCurrencyId, baseCurrencyCodeSnapshot,
            baseCurrencyDecimalPlacesSnapshot);
        RepriceLines();
    }

    public void AddLine(SalesInvoiceLine line)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(line);
        if (line.SalesInvoiceId != Id)
            throw new DomainException("Sales invoice line does not belong to this invoice.");
        if (_lines.Any(x => x.Id == line.Id || x.LineNumber == line.LineNumber))
            throw new DomainException("Duplicate sales invoice line is not allowed.");

        _lines.Add(line);
        RecalculateTotals();
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();
        var line = FindLine(lineId);
        _lines.Remove(line);
        ReNumberLines();
        RecalculateTotals();
    }

    public void UpdateLinePricing(
        Guid lineId, decimal quantity, decimal baseUnitPrice, decimal actualUnitPrice, SalesDiscountType discountType,
        decimal? discountValue, decimal? taxRate)
    {
        EnsureDraft();
        var line = FindLine(lineId);
        line.UpdatePricing(quantity, baseUnitPrice, actualUnitPrice, discountType, discountValue, taxRate,
            TaxCalculationMode, CurrencyDecimalPlacesSnapshot, ExchangeRate, BaseCurrencyDecimalPlacesSnapshot);
        RecalculateTotals();
    }

    public void SetLinePrescriptionSnapshot(Guid lineId, SalesInvoiceLinePrescriptionSnapshot snapshot)
    {
        EnsureDraft();
        FindLine(lineId).SetPrescriptionSnapshot(snapshot);
    }

    public void Confirm(DateTimeOffset confirmedAtUtc, string? confirmedBy)
    {
        EnsureDraft();
        if (_lines.Count == 0 || _lines.All(x => !x.IsActive))
            throw new DomainException("Sales invoice must contain at least one active line before confirmation.");

        RecalculateTotals();
        Status = SalesInvoiceStatus.Confirmed;
        ConfirmedAtUtc = confirmedAtUtc;
        ConfirmedBy = NormalizeUser(confirmedBy);
    }

    public void SetLineCostSnapshot(Guid lineId, decimal unitCost, decimal totalCost)
    {
        EnsureConfirmed();
        FindLine(lineId).SetCostSnapshot(unitCost, totalCost);
    }

    public void SetJournalEntry(Guid journalEntryId)
    {
        EnsureConfirmed();
        JournalEntryId = SalesDomainGuard.Required(journalEntryId, "Journal entry id");
    }

    public void Post(DateTimeOffset postedAtUtc, string? postedBy)
    {
        EnsureConfirmed();
        if (!JournalEntryId.HasValue)
            throw new DomainException("Journal entry must be assigned before posting the sales invoice.");
        if (_lines.Where(x => x.IsActive && x.RequiresInventory).Any(x => !x.UnitCostSnapshot.HasValue || !x.TotalCostSnapshot.HasValue))
            throw new DomainException("All inventory sales lines must have cost snapshots before posting.");

        Status = SalesInvoiceStatus.Posted;
        PostedAtUtc = postedAtUtc;
        PostedBy = NormalizeUser(postedBy);
    }

    public void Cancel(DateTimeOffset cancelledAtUtc, string? cancelledBy)
    {
        if (Status is SalesInvoiceStatus.Posted or SalesInvoiceStatus.Cancelled)
            throw new DomainException("Posted or already cancelled sales invoices cannot be cancelled directly.");

        Status = SalesInvoiceStatus.Cancelled;
        IsActive = false;
        CancelledAtUtc = cancelledAtUtc;
        CancelledBy = NormalizeUser(cancelledBy);
    }

    public void RecalculateTotals()
    {
        var activeLines = _lines.Where(x => x.IsActive).ToList();
        Subtotal = SalesPricingCalculator.Round(activeLines.Sum(x => x.GrossAmount), CurrencyDecimalPlacesSnapshot);
        DiscountAmount = SalesPricingCalculator.Round(activeLines.Sum(x => x.DiscountAmount), CurrencyDecimalPlacesSnapshot);
        TaxAmount = SalesPricingCalculator.Round(activeLines.Sum(x => x.TaxAmount), CurrencyDecimalPlacesSnapshot);
        TotalAmount = SalesPricingCalculator.Round(activeLines.Sum(x => x.FinalAmount), CurrencyDecimalPlacesSnapshot);

        BaseSubtotal = SalesPricingCalculator.ConvertToBase(Subtotal, ExchangeRate, BaseCurrencyDecimalPlacesSnapshot);
        BaseDiscountAmount = SalesPricingCalculator.ConvertToBase(DiscountAmount, ExchangeRate, BaseCurrencyDecimalPlacesSnapshot);
        BaseTaxAmount = SalesPricingCalculator.ConvertToBase(TaxAmount, ExchangeRate, BaseCurrencyDecimalPlacesSnapshot);
        BaseTotalAmount = SalesPricingCalculator.ConvertToBase(TotalAmount, ExchangeRate, BaseCurrencyDecimalPlacesSnapshot);
    }

    private void SetCurrencySnapshots(
        Guid currencyId, string code, string? symbol, byte decimals, decimal exchangeRate, DateOnly exchangeRateDate,
        ExchangeRateType exchangeRateType, ExchangeRateSource exchangeRateSource, Guid baseCurrencyId,
        string baseCurrencyCode, byte baseCurrencyDecimals)
    {
        CurrencyId = SalesDomainGuard.Required(currencyId, "Currency id");
        BaseCurrencyId = SalesDomainGuard.Required(baseCurrencyId, "Base currency id");
        CurrencyCodeSnapshot = SalesDomainGuard.Required(code, 10, "Currency code").ToUpperInvariant();
        CurrencySymbolSnapshot = SalesDomainGuard.Optional(symbol, 10, "Currency symbol");
        BaseCurrencyCodeSnapshot = SalesDomainGuard.Required(baseCurrencyCode, 10, "Base currency code").ToUpperInvariant();
        if (decimals > 6 || baseCurrencyDecimals > 6)
            throw new DomainException("Currency decimal places cannot exceed 6.");
        if (exchangeRate <= 0)
            throw new DomainException("Exchange rate must be greater than zero.");
        if (currencyId == baseCurrencyId && exchangeRate != 1m)
            throw new DomainException("Exchange rate must be 1 when invoice currency is the base currency.");
        SalesDomainGuard.Defined(exchangeRateType, "Exchange rate type");
        SalesDomainGuard.Defined(exchangeRateSource, "Exchange rate source");

        CurrencyDecimalPlacesSnapshot = decimals;
        ExchangeRate = exchangeRate;
        ExchangeRateDate = exchangeRateDate;
        ExchangeRateType = exchangeRateType;
        ExchangeRateSource = exchangeRateSource;
        BaseCurrencyDecimalPlacesSnapshot = baseCurrencyDecimals;
    }

    private void SetCommercialPolicy(TaxCalculationMode taxMode, SalesPaymentPlan paymentPlan, int paymentTermDays)
    {
        SalesDomainGuard.Defined(taxMode, "Tax calculation mode");
        SalesDomainGuard.Defined(paymentPlan, "Sales payment plan");

        TaxCalculationMode = taxMode;
        PaymentPlan = paymentPlan;
        PaymentTermType = SalesPaymentPlanPolicy.ToPaymentTermType(paymentPlan);
        PaymentTermDaysSnapshot = SalesPaymentPlanPolicy.NormalizePaymentTermDays(paymentPlan, paymentTermDays);
        DueDate = PaymentTermType == SalesPaymentTermType.Credit
            ? InvoiceDate.AddDays(PaymentTermDaysSnapshot)
            : null;
    }

    private void RepriceLines()
    {
        foreach (var line in _lines)
            line.UpdatePricing(line.Quantity, line.BaseUnitPrice, line.ActualUnitPrice, line.DiscountType, line.DiscountValue,
                line.TaxRate, TaxCalculationMode, CurrencyDecimalPlacesSnapshot, ExchangeRate, BaseCurrencyDecimalPlacesSnapshot);
        RecalculateTotals();
    }

    private SalesInvoiceLine FindLine(Guid lineId) =>
        _lines.SingleOrDefault(x => x.Id == lineId) ?? throw new DomainException("Sales invoice line was not found.");

    private void ReNumberLines()
    {
        var number = 1;
        foreach (var line in _lines.OrderBy(x => x.LineNumber))
            line.SetLineNumber(number++);
    }

    private void EnsureDraft()
    {
        if (Status != SalesInvoiceStatus.Draft)
            throw new DomainException("Only draft sales invoices can be modified.");
    }

    private void EnsureConfirmed()
    {
        if (Status != SalesInvoiceStatus.Confirmed)
            throw new DomainException("Only confirmed sales invoices can be posted.");
    }

    private static void ValidateOptionalId(Guid? id, string name)
    {
        if (id.HasValue && id.Value == Guid.Empty)
            throw new DomainException($"{name} cannot be empty.");
    }

    private static string? NormalizeUser(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
