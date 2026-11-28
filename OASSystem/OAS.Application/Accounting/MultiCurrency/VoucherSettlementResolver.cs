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
    public Task<VoucherSettlementResolution> ResolveAsync(
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
        CancellationToken cancellationToken = default) =>
        ResolveCoreAsync(voucherDate, partyType, customerId, supplierId, employeeId, partyName, otherCounterpartyAccountId, paymentMethod, cashAccountId, bankAccountId, otherSettlementAccountId, currencyId, amount, manualExchangeRate, exchangeRateType, referenceNumber, cancellationToken, null);

    public Task<VoucherSettlementResolution> ResolveAsync(
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
        CancellationToken cancellationToken, bool isReceipt) =>
        ResolveCoreAsync(voucherDate, partyType, customerId, supplierId, employeeId, partyName, otherCounterpartyAccountId, paymentMethod, cashAccountId, bankAccountId, otherSettlementAccountId, currencyId, amount, manualExchangeRate, exchangeRateType, referenceNumber, cancellationToken, isReceipt);

    private async Task<VoucherSettlementResolution> ResolveCoreAsync(
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
        CancellationToken cancellationToken, bool? isReceipt)
    {
        if (amount <= 0 || amount >= 1000000000000000m) throw new ConflictException("settlement_amount_invalid", "أدخل مبلغ تسوية أكبر من صفر وضمن الحدود المحاسبية.");
        var party = await counterparties.ResolveAsync(partyType, customerId, supplierId, employeeId, partyName, otherCounterpartyAccountId, cancellationToken);
        var settlement = isReceipt.HasValue
            ? await settlements.ResolveAsync(paymentMethod, currencyId, cashAccountId, bankAccountId, otherSettlementAccountId, referenceNumber, cancellationToken, isReceipt.Value)
            : await settlements.ResolveAsync(paymentMethod, currencyId, cashAccountId, bankAccountId, otherSettlementAccountId, referenceNumber, cancellationToken);
        if (party.AccountId == settlement.AccountId)
            throw new ConflictException("voucher_same_accounts", "لا يجوز أن يكون حساب الطرف هو حساب التسوية نفسه.");
        var allowManual = manualExchangeRate.HasValue && await permissions.HasPermissionAsync(AccountingPermissions.ExchangeRates.Override, cancellationToken);
        var rate = await rates.ResolveAsync(currencyId, voucherDate, exchangeRateType, manualExchangeRate, allowManual, cancellationToken);
        var baseDecimalPlaces = await ResolveBaseDecimalPlacesAsync(cancellationToken);
        if (rate.CurrencyDecimalPlaces > 4 || baseDecimalPlaces > 4 || rate.Rate != Math.Round(rate.Rate, 8))
            throw new ConflictException("voucher_currency_precision", "دقة عملة السند أو سعر الصرف تتجاوز الدقة المحاسبية المخزنة.");
        decimal baseAmount;
        try { baseAmount = rounding.CalculateBaseAmount(amount, rate.Rate, rate.CurrencyDecimalPlaces, baseDecimalPlaces); }
        catch (OverflowException) { throw new ConflictException("voucher_amount_overflow", "القيمة الأساسية تتجاوز الحدود المحاسبية."); }
        if (baseAmount >= 1000000000000000m)
            throw new ConflictException("voucher_amount_overflow", "القيمة الأساسية تتجاوز الحدود المحاسبية.");
        if (baseAmount <= 0 || rounding.Round(amount, rate.CurrencyDecimalPlaces) <= 0)
            throw new ConflictException("voucher_amount_rounds_to_zero", "المبلغ بعد التقريب أو قيمته الأساسية يساوي صفرًا.");
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
