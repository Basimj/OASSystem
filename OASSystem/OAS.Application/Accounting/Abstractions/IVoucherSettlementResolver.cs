using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Abstractions;

public sealed record VoucherSettlementResolution(
    SettlementPartyType PartyType,
    Guid? CustomerId,
    Guid? SupplierId,
    Guid? EmployeeId,
    string PartyNameSnapshot,
    Guid CounterpartyAccountId,
    PaymentMethod PaymentMethod,
    Guid? CashAccountId,
    Guid? BankAccountId,
    Guid SettlementAccountId,
    Guid CurrencyId,
    string CurrencyCode,
    string? CurrencySymbol,
    byte CurrencyDecimalPlaces,
    decimal Amount,
    decimal ExchangeRate,
    DateOnly ExchangeRateDate,
    ExchangeRateType ExchangeRateType,
    ExchangeRateSource ExchangeRateSource,
    decimal BaseAmount);

public interface IVoucherSettlementResolver
{
    Task<VoucherSettlementResolution> ResolveAsync(
        DateOnly voucherDate,
        SettlementPartyType partyType,
        Guid? customerId,
        Guid? supplierId,
        Guid? employeeId,
        string? partyName,
        Guid? otherCounterpartyAccountId,
        PaymentMethod paymentMethod,
        Guid? cashAccountId,
        Guid? bankAccountId,
        Guid? otherSettlementAccountId,
        Guid currencyId,
        decimal amount,
        decimal? manualExchangeRate,
        ExchangeRateType exchangeRateType,
        string? referenceNumber,
        CancellationToken cancellationToken = default);
}
