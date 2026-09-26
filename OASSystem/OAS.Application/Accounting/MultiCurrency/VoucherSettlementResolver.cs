using OAS.Application.Abstractions.Security;
using OAS.Application.Abstractions.Persistence;
using OAS.Domain.Accounting.Entities;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.MultiCurrency;

public sealed class VoucherSettlementResolver(
    ICounterpartyAccountResolver counterparties,
    ISettlementAccountResolver settlements,
    IExchangeRateResolver rates,
    ICurrencyRoundingService rounding,
    IPermissionChecker permissions,
    IReadRepository<AccountingSettings, Guid> settingsRepository,
    IReadRepository<Currency, Guid> currencies) : IVoucherSettlementResolver
{
    public async Task<VoucherSettlementResolution> ResolveAsync(
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
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0) throw new ConflictException("settlement_amount_invalid", "Settlement line amount must be greater than zero.");
        var party = await counterparties.ResolveAsync(partyType, customerId, supplierId, employeeId, partyName, otherCounterpartyAccountId, cancellationToken);
        var settlement = await settlements.ResolveAsync(paymentMethod, currencyId, cashAccountId, bankAccountId, otherSettlementAccountId, referenceNumber, cancellationToken);
        var allowManual = manualExchangeRate.HasValue && await permissions.HasPermissionAsync(AccountingPermissions.ExchangeRates.Override, cancellationToken);
        var rate = await rates.ResolveAsync(currencyId, voucherDate, exchangeRateType, manualExchangeRate, allowManual, cancellationToken);
        var baseDecimalPlaces = await ResolveBaseDecimalPlacesAsync(cancellationToken);
        var baseAmount = rounding.CalculateBaseAmount(amount, rate.Rate, rate.CurrencyDecimalPlaces, baseDecimalPlaces);
        return new(partyType, party.CustomerId, party.SupplierId, party.EmployeeId, party.PartyNameSnapshot, party.AccountId,
            paymentMethod, settlement.CashAccountId, settlement.BankAccountId, settlement.AccountId,
            rate.CurrencyId, rate.CurrencyCode, rate.CurrencySymbol, rate.CurrencyDecimalPlaces,
            rounding.Round(amount, rate.CurrencyDecimalPlaces), rate.Rate, rate.RateDate, rate.RateType, rate.Source, baseAmount);
    }

    private async Task<byte> ResolveBaseDecimalPlacesAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetByIdAsync(AccountingSettings.SingletonId, cancellationToken)
            ?? throw new ConflictException("accounting_settings_required", "Accounting settings and base currency must be configured first.");
        var baseCurrency = await currencies.GetByIdAsync(settings.BaseCurrencyId, cancellationToken)
            ?? throw new ConflictException("base_currency_missing", "Configured base currency does not exist.");
        return baseCurrency.DecimalPlaces;
    }
}
