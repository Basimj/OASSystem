using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class PaymentVoucherLine : AuditableEntity<Guid>
{
    private PaymentVoucherLine() { }

    public Guid PaymentVoucherId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid AccountId { get; private set; }
    public decimal Amount { get; private set; }

    public SettlementPartyType? PartyType { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public Guid? EmployeeId { get; private set; }
    public string? PartyNameSnapshot { get; private set; }
    public Guid? CounterpartyAccountId { get; private set; }

    public PaymentMethod? PaymentMethod { get; private set; }
    public Guid? CashAccountId { get; private set; }
    public Guid? BankAccountId { get; private set; }
    public Guid? SettlementAccountId { get; private set; }

    public Guid? CurrencyId { get; private set; }
    public string? CurrencyCodeSnapshot { get; private set; }
    public string? CurrencySymbolSnapshot { get; private set; }
    public byte? CurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public DateOnly? ExchangeRateDate { get; private set; }
    public ExchangeRateType? ExchangeRateType { get; private set; }
    public ExchangeRateSource? ExchangeRateSource { get; private set; }
    public decimal? BaseAmount { get; private set; }

    public string? ReferenceNumber { get; private set; }
    public DateOnly? ReferenceDate { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? Description { get; private set; }

    public static PaymentVoucherLine CreateSettlement(
        Guid id,
        Guid paymentVoucherId,
        int lineNumber,
        SettlementPartyType partyType,
        Guid? customerId,
        Guid? supplierId,
        Guid? employeeId,
        string partyNameSnapshot,
        Guid counterpartyAccountId,
        PaymentMethod paymentMethod,
        Guid? cashAccountId,
        Guid? bankAccountId,
        Guid settlementAccountId,
        Guid currencyId,
        string currencyCodeSnapshot,
        string? currencySymbolSnapshot,
        byte currencyDecimalPlacesSnapshot,
        decimal amount,
        decimal exchangeRate,
        DateOnly exchangeRateDate,
        ExchangeRateType exchangeRateType,
        ExchangeRateSource exchangeRateSource,
        decimal baseAmount,
        string? referenceNumber,
        DateOnly? referenceDate,
        string? referenceType,
        Guid? referenceId,
        string? description)
    {
        ValidateIdentity(id, paymentVoucherId, lineNumber);
        ValidateSettlement(
            partyType, customerId, supplierId, employeeId, partyNameSnapshot,
            counterpartyAccountId, settlementAccountId, currencyId,
            currencyCodeSnapshot, currencyDecimalPlacesSnapshot, amount,
            exchangeRate, baseAmount);

        return new PaymentVoucherLine
        {
            Id = id,
            PaymentVoucherId = paymentVoucherId,
            LineNumber = lineNumber,
            AccountId = counterpartyAccountId,
            PartyType = partyType,
            CustomerId = customerId,
            SupplierId = supplierId,
            EmployeeId = employeeId,
            PartyNameSnapshot = Required(partyNameSnapshot, nameof(partyNameSnapshot)),
            CounterpartyAccountId = counterpartyAccountId,
            PaymentMethod = paymentMethod,
            CashAccountId = Normalize(cashAccountId),
            BankAccountId = Normalize(bankAccountId),
            SettlementAccountId = settlementAccountId,
            CurrencyId = currencyId,
            CurrencyCodeSnapshot = Required(currencyCodeSnapshot, nameof(currencyCodeSnapshot)),
            CurrencySymbolSnapshot = Optional(currencySymbolSnapshot),
            CurrencyDecimalPlacesSnapshot = currencyDecimalPlacesSnapshot,
            Amount = amount,
            ExchangeRate = exchangeRate,
            ExchangeRateDate = exchangeRateDate,
            ExchangeRateType = exchangeRateType,
            ExchangeRateSource = exchangeRateSource,
            BaseAmount = baseAmount,
            ReferenceNumber = Optional(referenceNumber),
            ReferenceDate = referenceDate,
            ReferenceType = Optional(referenceType),
            ReferenceId = Normalize(referenceId),
            Description = Optional(description)
        };
    }

    public static PaymentVoucherLine Create(
        Guid id,
        Guid paymentVoucherId,
        int lineNumber,
        Guid accountId,
        decimal amount,
        string? referenceType,
        Guid? referenceId,
        string? description)
    {
        ValidateIdentity(id, paymentVoucherId, lineNumber);
        if (accountId == Guid.Empty) throw new ArgumentException("Account id is required.", nameof(accountId));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        return new PaymentVoucherLine
        {
            Id = id,
            PaymentVoucherId = paymentVoucherId,
            LineNumber = lineNumber,
            AccountId = accountId,
            Amount = amount,
            ReferenceType = Optional(referenceType),
            ReferenceId = Normalize(referenceId),
            Description = Optional(description)
        };
    }

    internal void SetLineNumber(int lineNumber)
    {
        if (lineNumber <= 0) throw new ArgumentOutOfRangeException(nameof(lineNumber));
        LineNumber = lineNumber;
    }

    private static void ValidateIdentity(Guid id, Guid voucherId, int lineNumber)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (voucherId == Guid.Empty) throw new ArgumentException("Payment voucher id is required.", nameof(voucherId));
        if (lineNumber <= 0) throw new ArgumentOutOfRangeException(nameof(lineNumber));
    }

    private static void ValidateSettlement(
        SettlementPartyType partyType,
        Guid? customerId,
        Guid? supplierId,
        Guid? employeeId,
        string partyNameSnapshot,
        Guid counterpartyAccountId,
        Guid settlementAccountId,
        Guid currencyId,
        string currencyCodeSnapshot,
        byte decimalPlaces,
        decimal amount,
        decimal exchangeRate,
        decimal baseAmount)
    {
        var typedCount = (customerId.HasValue ? 1 : 0) + (supplierId.HasValue ? 1 : 0) + (employeeId.HasValue ? 1 : 0);
        var validParty = partyType switch
        {
            SettlementPartyType.Customer => typedCount == 1 && customerId.HasValue,
            SettlementPartyType.Supplier => typedCount == 1 && supplierId.HasValue,
            SettlementPartyType.Employee => typedCount == 1 && employeeId.HasValue,
            SettlementPartyType.Other => typedCount == 0,
            _ => false
        };

        if (!validParty) throw new InvalidOperationException("Typed party foreign keys do not match PartyType.");
        if (string.IsNullOrWhiteSpace(partyNameSnapshot)) throw new ArgumentException("Party snapshot is required.", nameof(partyNameSnapshot));
        if (counterpartyAccountId == Guid.Empty) throw new ArgumentException("Counterparty account is required.", nameof(counterpartyAccountId));
        if (settlementAccountId == Guid.Empty) throw new ArgumentException("Settlement account is required.", nameof(settlementAccountId));
        if (currencyId == Guid.Empty) throw new ArgumentException("Currency is required.", nameof(currencyId));
        if (string.IsNullOrWhiteSpace(currencyCodeSnapshot)) throw new ArgumentException("Currency snapshot is required.", nameof(currencyCodeSnapshot));
        if (decimalPlaces > 6) throw new ArgumentOutOfRangeException(nameof(decimalPlaces));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (exchangeRate <= 0) throw new ArgumentOutOfRangeException(nameof(exchangeRate));
        if (baseAmount <= 0) throw new ArgumentOutOfRangeException(nameof(baseAmount));
    }

    private static Guid? Normalize(Guid? value) => value is { } id && id != Guid.Empty ? id : null;
    private static string Required(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} is required.", name) : value.Trim();
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
