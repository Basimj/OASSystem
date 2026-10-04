using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class CustomerAdvance : AuditableEntity<Guid>
{
    private CustomerAdvance() { }

    private CustomerAdvance(
        Guid id,
        string advanceNumber,
        Guid customerId,
        Guid customerOrderId,
        Guid receiptVoucherLineId,
        Guid currencyId,
        string currencyCodeSnapshot,
        string? currencySymbolSnapshot,
        byte currencyDecimalPlacesSnapshot,
        Guid baseCurrencyId,
        string baseCurrencyCodeSnapshot,
        byte baseCurrencyDecimalPlacesSnapshot,
        decimal amount,
        decimal exchangeRate,
        DateOnly exchangeRateDate,
        ExchangeRateType exchangeRateType,
        ExchangeRateSource exchangeRateSource,
        decimal baseAmount,
        DateTime receivedAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Customer advance id is required.", nameof(id));
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id is required.", nameof(customerId));
        if (customerOrderId == Guid.Empty) throw new ArgumentException("Customer order id is required.", nameof(customerOrderId));
        if (receiptVoucherLineId == Guid.Empty) throw new ArgumentException("Receipt voucher line id is required.", nameof(receiptVoucherLineId));
        if (currencyId == Guid.Empty) throw new ArgumentException("Currency id is required.", nameof(currencyId));
        if (baseCurrencyId == Guid.Empty) throw new ArgumentException("Base currency id is required.", nameof(baseCurrencyId));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Advance amount must be greater than zero.");
        if (baseAmount <= 0) throw new ArgumentOutOfRangeException(nameof(baseAmount), "Advance base amount must be greater than zero.");
        if (exchangeRate <= 0) throw new ArgumentOutOfRangeException(nameof(exchangeRate), "Exchange rate must be greater than zero.");
        if (currencyDecimalPlacesSnapshot > 6) throw new ArgumentOutOfRangeException(nameof(currencyDecimalPlacesSnapshot));
        if (baseCurrencyDecimalPlacesSnapshot > 6) throw new ArgumentOutOfRangeException(nameof(baseCurrencyDecimalPlacesSnapshot));
        if (!Enum.IsDefined(exchangeRateType)) throw new ArgumentOutOfRangeException(nameof(exchangeRateType));
        if (!Enum.IsDefined(exchangeRateSource)) throw new ArgumentOutOfRangeException(nameof(exchangeRateSource));

        Id = id;
        AdvanceNumber = Required(advanceNumber, 40, nameof(advanceNumber));
        CustomerId = customerId;
        CustomerOrderId = customerOrderId;
        ReceiptVoucherLineId = receiptVoucherLineId;
        CurrencyId = currencyId;
        CurrencyCodeSnapshot = Required(currencyCodeSnapshot, 8, nameof(currencyCodeSnapshot)).ToUpperInvariant();
        CurrencySymbolSnapshot = Optional(currencySymbolSnapshot, 12, nameof(currencySymbolSnapshot));
        CurrencyDecimalPlacesSnapshot = currencyDecimalPlacesSnapshot;
        BaseCurrencyId = baseCurrencyId;
        BaseCurrencyCodeSnapshot = Required(baseCurrencyCodeSnapshot, 8, nameof(baseCurrencyCodeSnapshot)).ToUpperInvariant();
        BaseCurrencyDecimalPlacesSnapshot = baseCurrencyDecimalPlacesSnapshot;
        Amount = amount;
        ExchangeRate = exchangeRate;
        ExchangeRateDate = exchangeRateDate;
        ExchangeRateType = exchangeRateType;
        ExchangeRateSource = exchangeRateSource;
        BaseAmount = baseAmount;
        AppliedAmount = 0m;
        BaseAppliedAmount = 0m;
        Status = CustomerAdvanceStatus.Available;
        ReceivedAtUtc = receivedAtUtc;
        IsActive = true;
    }

    public string AdvanceNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Guid CustomerOrderId { get; private set; }
    public Guid ReceiptVoucherLineId { get; private set; }
    public Guid CurrencyId { get; private set; }
    public string CurrencyCodeSnapshot { get; private set; } = string.Empty;
    public string? CurrencySymbolSnapshot { get; private set; }
    public byte CurrencyDecimalPlacesSnapshot { get; private set; }
    public Guid BaseCurrencyId { get; private set; }
    public string BaseCurrencyCodeSnapshot { get; private set; } = string.Empty;
    public byte BaseCurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal Amount { get; private set; }
    public decimal ExchangeRate { get; private set; }
    public DateOnly ExchangeRateDate { get; private set; }
    public ExchangeRateType ExchangeRateType { get; private set; }
    public ExchangeRateSource ExchangeRateSource { get; private set; }
    public decimal BaseAmount { get; private set; }
    public decimal AppliedAmount { get; private set; }
    public decimal BaseAppliedAmount { get; private set; }
    public CustomerAdvanceStatus Status { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public decimal AvailableAmount => Amount - AppliedAmount;
    public decimal BaseAvailableAmount => BaseAmount - BaseAppliedAmount;

    public static CustomerAdvance Create(
        Guid id,
        string advanceNumber,
        Guid customerId,
        Guid customerOrderId,
        Guid receiptVoucherLineId,
        Guid currencyId,
        string currencyCodeSnapshot,
        string? currencySymbolSnapshot,
        byte currencyDecimalPlacesSnapshot,
        Guid baseCurrencyId,
        string baseCurrencyCodeSnapshot,
        byte baseCurrencyDecimalPlacesSnapshot,
        decimal amount,
        decimal exchangeRate,
        DateOnly exchangeRateDate,
        ExchangeRateType exchangeRateType,
        ExchangeRateSource exchangeRateSource,
        decimal baseAmount,
        DateTime receivedAtUtc) =>
        new(
            id,
            advanceNumber,
            customerId,
            customerOrderId,
            receiptVoucherLineId,
            currencyId,
            currencyCodeSnapshot,
            currencySymbolSnapshot,
            currencyDecimalPlacesSnapshot,
            baseCurrencyId,
            baseCurrencyCodeSnapshot,
            baseCurrencyDecimalPlacesSnapshot,
            amount,
            exchangeRate,
            exchangeRateDate,
            exchangeRateType,
            exchangeRateSource,
            baseAmount,
            receivedAtUtc);

    public void Apply(decimal amount, decimal baseAmount)
    {
        EnsureApplicable();
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Applied amount must be greater than zero.");
        if (baseAmount <= 0) throw new ArgumentOutOfRangeException(nameof(baseAmount), "Applied base amount must be greater than zero.");
        if (AppliedAmount + amount > Amount)
            throw new InvalidOperationException("Applied amount cannot exceed the customer advance amount.");
        if (BaseAppliedAmount + baseAmount > BaseAmount)
            throw new InvalidOperationException("Applied base amount cannot exceed the customer advance base amount.");

        AppliedAmount += amount;
        BaseAppliedAmount += baseAmount;
        RefreshStatus();
    }

    public void ReverseApplication(decimal amount, decimal baseAmount)
    {
        if (Status is CustomerAdvanceStatus.Cancelled or CustomerAdvanceStatus.Refunded)
            throw new InvalidOperationException("A cancelled or refunded customer advance cannot reverse applications.");
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (baseAmount <= 0) throw new ArgumentOutOfRangeException(nameof(baseAmount));
        if (amount > AppliedAmount || baseAmount > BaseAppliedAmount)
            throw new InvalidOperationException("Cannot reverse more than the amount already applied.");

        AppliedAmount -= amount;
        BaseAppliedAmount -= baseAmount;
        RefreshStatus();
    }

    public void MarkRefunded()
    {
        if (Status is CustomerAdvanceStatus.Cancelled or CustomerAdvanceStatus.Refunded)
            throw new InvalidOperationException("Customer advance is already closed.");
        if (AppliedAmount != 0m || BaseAppliedAmount != 0m)
            throw new InvalidOperationException("An applied customer advance must be reversed before it can be fully refunded.");

        Status = CustomerAdvanceStatus.Refunded;
        IsActive = false;
    }

    public void Cancel()
    {
        if (Status is CustomerAdvanceStatus.Applied or CustomerAdvanceStatus.PartiallyApplied)
            throw new InvalidOperationException("An applied customer advance cannot be cancelled directly.");
        if (Status == CustomerAdvanceStatus.Refunded)
            throw new InvalidOperationException("A refunded customer advance cannot be cancelled.");
        Status = CustomerAdvanceStatus.Cancelled;
        IsActive = false;
    }

    private void EnsureApplicable()
    {
        if (!IsActive || Status is CustomerAdvanceStatus.Applied or CustomerAdvanceStatus.Refunded or CustomerAdvanceStatus.Cancelled)
            throw new InvalidOperationException("Customer advance is not available for application.");
    }

    private void RefreshStatus()
    {
        Status = AppliedAmount switch
        {
            0m => CustomerAdvanceStatus.Available,
            _ when AppliedAmount == Amount => CustomerAdvanceStatus.Applied,
            _ => CustomerAdvanceStatus.PartiallyApplied
        };
    }

    private static string Required(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{name} is required.", name);
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentOutOfRangeException(name, $"{name} cannot exceed {maxLength} characters.");
        return normalized;
    }

    private static string? Optional(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentOutOfRangeException(name, $"{name} cannot exceed {maxLength} characters.");
        return normalized;
    }
}
