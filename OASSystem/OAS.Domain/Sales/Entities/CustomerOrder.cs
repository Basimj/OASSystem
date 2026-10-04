using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class CustomerOrder : AuditableEntity<Guid>
{
    private readonly List<CustomerOrderLine> _lines = [];

    private CustomerOrder() { }

    private CustomerOrder(
        Guid id, string orderCode, Guid customerId, Guid? prescriptionRevisionId, DateOnly orderDate,
        DateOnly? requiredDate, Guid currencyId, string currencyCodeSnapshot, string? currencySymbolSnapshot,
        byte currencyDecimalPlacesSnapshot, decimal exchangeRate, DateOnly exchangeRateDate,
        ExchangeRateType exchangeRateType, ExchangeRateSource exchangeRateSource, TaxCalculationMode taxCalculationMode,
        SalesPaymentTermType paymentTermType, int paymentTermDaysSnapshot, string? notes)
    {
        Id = SalesDomainGuard.Required(id, "Customer order id");
        OrderCode = SalesDomainGuard.Required(orderCode, 40, "Order code");
        CustomerId = SalesDomainGuard.Required(customerId, "Customer id");
        if (prescriptionRevisionId == Guid.Empty)
            throw new DomainException("Prescription revision id cannot be empty.");
        if (requiredDate.HasValue && requiredDate.Value < orderDate)
            throw new DomainException("Required date cannot be before order date.");

        PrescriptionRevisionId = prescriptionRevisionId;
        OrderDate = orderDate;
        RequiredDate = requiredDate;
        SetCurrencySnapshot(currencyId, currencyCodeSnapshot, currencySymbolSnapshot, currencyDecimalPlacesSnapshot, exchangeRate, exchangeRateDate, exchangeRateType, exchangeRateSource);
        SetCommercialPolicy(taxCalculationMode, paymentTermType, paymentTermDaysSnapshot);
        Notes = SalesDomainGuard.Optional(notes, 1000, "Order notes");
        Status = CustomerOrderStatus.Draft;
        IsActive = true;
    }

    public string OrderCode { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Guid? PrescriptionRevisionId { get; private set; }
    public DateOnly OrderDate { get; private set; }
    public DateOnly? RequiredDate { get; private set; }
    public CustomerOrderStatus Status { get; private set; }
    public Guid CurrencyId { get; private set; }
    public string CurrencyCodeSnapshot { get; private set; } = string.Empty;
    public string? CurrencySymbolSnapshot { get; private set; }
    public byte CurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal ExchangeRate { get; private set; }
    public DateOnly ExchangeRateDate { get; private set; }
    public ExchangeRateType ExchangeRateType { get; private set; }
    public ExchangeRateSource ExchangeRateSource { get; private set; }
    public TaxCalculationMode TaxCalculationMode { get; private set; }
    public SalesPaymentTermType PaymentTermType { get; private set; }
    public int PaymentTermDaysSnapshot { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? ConfirmedAtUtc { get; private set; }
    public string? ConfirmedBy { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancelledBy { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<CustomerOrderLine> Lines => _lines.AsReadOnly();

    public static CustomerOrder Create(
        Guid id, string orderCode, Guid customerId, Guid? prescriptionRevisionId, DateOnly orderDate,
        DateOnly? requiredDate, Guid currencyId, string currencyCodeSnapshot, string? currencySymbolSnapshot,
        byte currencyDecimalPlacesSnapshot, decimal exchangeRate, DateOnly exchangeRateDate,
        ExchangeRateType exchangeRateType, ExchangeRateSource exchangeRateSource, TaxCalculationMode taxCalculationMode,
        SalesPaymentTermType paymentTermType, int paymentTermDaysSnapshot, string? notes = null) =>
        new(id, orderCode, customerId, prescriptionRevisionId, orderDate, requiredDate, currencyId, currencyCodeSnapshot,
            currencySymbolSnapshot, currencyDecimalPlacesSnapshot, exchangeRate, exchangeRateDate, exchangeRateType,
            exchangeRateSource, taxCalculationMode, paymentTermType, paymentTermDaysSnapshot, notes);

    public void UpdateHeader(
        Guid customerId, Guid? prescriptionRevisionId, DateOnly orderDate, DateOnly? requiredDate,
        TaxCalculationMode taxCalculationMode, SalesPaymentTermType paymentTermType, int paymentTermDaysSnapshot,
        string? notes)
    {
        EnsureDraft();
        CustomerId = SalesDomainGuard.Required(customerId, "Customer id");
        if (prescriptionRevisionId == Guid.Empty)
            throw new DomainException("Prescription revision id cannot be empty.");
        if (requiredDate.HasValue && requiredDate.Value < orderDate)
            throw new DomainException("Required date cannot be before order date.");

        PrescriptionRevisionId = prescriptionRevisionId;
        OrderDate = orderDate;
        RequiredDate = requiredDate;
        SetCommercialPolicy(taxCalculationMode, paymentTermType, paymentTermDaysSnapshot);
        Notes = SalesDomainGuard.Optional(notes, 1000, "Order notes");
        RepriceLines();
    }

    public void ChangeCurrency(
        Guid currencyId, string currencyCodeSnapshot, string? currencySymbolSnapshot, byte currencyDecimalPlacesSnapshot,
        decimal exchangeRate, DateOnly exchangeRateDate, ExchangeRateType exchangeRateType, ExchangeRateSource exchangeRateSource)
    {
        EnsureDraft();
        SetCurrencySnapshot(currencyId, currencyCodeSnapshot, currencySymbolSnapshot, currencyDecimalPlacesSnapshot,
            exchangeRate, exchangeRateDate, exchangeRateType, exchangeRateSource);
        RepriceLines();
    }

    public void AddLine(CustomerOrderLine line)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(line);
        if (line.CustomerOrderId != Id)
            throw new DomainException("Customer order line does not belong to this order.");
        if (_lines.Any(x => x.Id == line.Id || x.LineNumber == line.LineNumber))
            throw new DomainException("Duplicate customer order line is not allowed.");

        _lines.Add(line);
        RecalculateTotals();
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();
        var line = _lines.SingleOrDefault(x => x.Id == lineId)
            ?? throw new DomainException("Customer order line was not found.");
        _lines.Remove(line);
        ReNumberLines();
        RecalculateTotals();
    }

    public void UpdateLinePricing(
        Guid lineId, decimal quantity, decimal baseUnitPrice, decimal actualUnitPrice,
        SalesDiscountType discountType, decimal? discountValue, decimal? taxRate)
    {
        EnsureDraft();
        var line = _lines.SingleOrDefault(x => x.Id == lineId)
            ?? throw new DomainException("Customer order line was not found.");
        line.UpdatePricing(quantity, baseUnitPrice, actualUnitPrice, discountType, discountValue, taxRate,
            TaxCalculationMode, CurrencyDecimalPlacesSnapshot);
        RecalculateTotals();
    }

    public void Confirm(CustomerOrderStatus resultingStatus, DateTimeOffset confirmedAtUtc, string? confirmedBy)
    {
        EnsureDraft();
        if (_lines.Count == 0)
            throw new DomainException("Customer order must contain at least one line before confirmation.");
        if (resultingStatus is not (CustomerOrderStatus.Confirmed or CustomerOrderStatus.AwaitingStock or CustomerOrderStatus.PartiallyAvailable or CustomerOrderStatus.ReadyForProduction))
            throw new DomainException("Invalid customer order confirmation status.");

        RecalculateTotals();
        Status = resultingStatus;
        ConfirmedAtUtc = confirmedAtUtc;
        ConfirmedBy = NormalizeUser(confirmedBy);
    }

    public void SetAvailabilityStatus(CustomerOrderStatus status)
    {
        if (Status is CustomerOrderStatus.Draft or CustomerOrderStatus.Cancelled or CustomerOrderStatus.Completed)
            throw new DomainException("Customer order availability status cannot be changed in the current state.");
        if (status is not (CustomerOrderStatus.Confirmed or CustomerOrderStatus.AwaitingStock or CustomerOrderStatus.PartiallyAvailable or CustomerOrderStatus.ReadyForProduction))
            throw new DomainException("Invalid stock availability status.");
        Status = status;
    }

    public void StartProduction()
    {
        if (Status != CustomerOrderStatus.ReadyForProduction)
            throw new DomainException("Customer order can only enter production after all required materials are available.");
        Status = CustomerOrderStatus.InProduction;
    }

    public void MarkReadyForDelivery()
    {
        if (Status != CustomerOrderStatus.InProduction)
            throw new DomainException("Customer order can only become ready for delivery after production has started.");
        Status = CustomerOrderStatus.ReadyForDelivery;
    }

    public void Complete()
    {
        if (Status != CustomerOrderStatus.ReadyForDelivery)
            throw new DomainException("Customer order can only be completed when it is ready for delivery.");
        Status = CustomerOrderStatus.Completed;
        IsActive = false;
    }

    public void Cancel(DateTimeOffset cancelledAtUtc, string? cancelledBy)
    {
        if (Status is CustomerOrderStatus.Cancelled or CustomerOrderStatus.Completed or CustomerOrderStatus.InProduction or CustomerOrderStatus.ReadyForDelivery)
            throw new DomainException("Customer order cannot be cancelled in the current state.");

        Status = CustomerOrderStatus.Cancelled;
        IsActive = false;
        CancelledAtUtc = cancelledAtUtc;
        CancelledBy = NormalizeUser(cancelledBy);
    }

    public void RecalculateTotals()
    {
        Subtotal = SalesPricingCalculator.Round(_lines.Where(x => x.IsActive).Sum(x => x.GrossAmount), CurrencyDecimalPlacesSnapshot);
        DiscountAmount = SalesPricingCalculator.Round(_lines.Where(x => x.IsActive).Sum(x => x.DiscountAmount), CurrencyDecimalPlacesSnapshot);
        TaxAmount = SalesPricingCalculator.Round(_lines.Where(x => x.IsActive).Sum(x => x.TaxAmount), CurrencyDecimalPlacesSnapshot);
        TotalAmount = SalesPricingCalculator.Round(_lines.Where(x => x.IsActive).Sum(x => x.FinalAmount), CurrencyDecimalPlacesSnapshot);
    }

    private void SetCurrencySnapshot(
        Guid currencyId, string code, string? symbol, byte decimalPlaces, decimal exchangeRate, DateOnly rateDate,
        ExchangeRateType rateType, ExchangeRateSource rateSource)
    {
        CurrencyId = SalesDomainGuard.Required(currencyId, "Currency id");
        CurrencyCodeSnapshot = SalesDomainGuard.Required(code, 10, "Currency code").ToUpperInvariant();
        CurrencySymbolSnapshot = SalesDomainGuard.Optional(symbol, 10, "Currency symbol");
        if (decimalPlaces > 6) throw new DomainException("Currency decimal places cannot exceed 6.");
        if (exchangeRate <= 0) throw new DomainException("Exchange rate must be greater than zero.");
        SalesDomainGuard.Defined(rateType, "Exchange rate type");
        SalesDomainGuard.Defined(rateSource, "Exchange rate source");
        CurrencyDecimalPlacesSnapshot = decimalPlaces;
        ExchangeRate = exchangeRate;
        ExchangeRateDate = rateDate;
        ExchangeRateType = rateType;
        ExchangeRateSource = rateSource;
    }

    private void SetCommercialPolicy(TaxCalculationMode taxMode, SalesPaymentTermType paymentTermType, int paymentTermDays)
    {
        SalesDomainGuard.Defined(taxMode, "Tax calculation mode");
        SalesDomainGuard.Defined(paymentTermType, "Payment term type");
        if (paymentTermDays < 0) throw new DomainException("Payment term days cannot be negative.");
        TaxCalculationMode = taxMode;
        PaymentTermType = paymentTermType;
        PaymentTermDaysSnapshot = paymentTermType == SalesPaymentTermType.Immediate ? 0 : paymentTermDays;
    }

    private void RepriceLines()
    {
        foreach (var line in _lines)
            line.UpdatePricing(line.Quantity, line.BaseUnitPrice, line.ActualUnitPrice, line.DiscountType, line.DiscountValue, line.TaxRate, TaxCalculationMode, CurrencyDecimalPlacesSnapshot);
        RecalculateTotals();
    }

    private void ReNumberLines()
    {
        var number = 1;
        foreach (var line in _lines.OrderBy(x => x.LineNumber))
            line.SetLineNumber(number++);
    }

    private void EnsureDraft()
    {
        if (Status != CustomerOrderStatus.Draft)
            throw new DomainException("Only draft customer orders can be modified.");
    }

    private static string? NormalizeUser(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
