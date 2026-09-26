using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.MultiCurrency;

public sealed class ExchangeRateResolver(
    IReadRepository<Currency, Guid> currencies,
    IReadRepository<ExchangeRate, Guid> rates,
    IReadRepository<AccountingSettings, Guid> settingsRepository) : IExchangeRateResolver
{
    public async Task<ExchangeRateResolution> ResolveAsync(
        Guid currencyId,
        DateOnly documentDate,
        ExchangeRateType rateType = ExchangeRateType.Accounting,
        decimal? manualRate = null,
        bool manualOverrideAllowed = false,
        CancellationToken cancellationToken = default)
    {
        if (currencyId == Guid.Empty) throw new ConflictException("currency_required", "Currency is required.");

        var settings = await settingsRepository.GetByIdAsync(AccountingSettings.SingletonId, cancellationToken)
            ?? throw new ConflictException("accounting_settings_required", "Accounting settings and base currency must be configured first.");
        var currency = await currencies.GetByIdAsync(currencyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), currencyId);

        if (!currency.IsActive)
            throw new ConflictException("currency_inactive", $"Currency '{currency.Code}' is inactive.");

        var isBase = settings.BaseCurrencyId == currencyId;
        if (isBase)
        {
            if (manualRate.HasValue && manualRate.Value != 1m)
                throw new ConflictException("base_currency_rate_must_be_one", "Base currency exchange rate must equal 1.");
            return new(currency.Id, currency.Code, currency.Symbol, currency.DecimalPlaces, 1m, documentDate, rateType, ExchangeRateSource.System, true);
        }

        if (manualRate.HasValue)
        {
            if (!manualOverrideAllowed)
                throw new ConflictException("exchange_rate_override_forbidden", "Manual exchange rate override is not allowed.");
            if (manualRate.Value <= 0)
                throw new ConflictException("exchange_rate_invalid", "Exchange rate must be greater than zero.");
            return new(currency.Id, currency.Code, currency.Symbol, currency.DecimalPlaces, manualRate.Value, documentDate, rateType, ExchangeRateSource.Manual, false);
        }

        var spec = new Specification<ExchangeRate>()
            .Where(x => x.CurrencyId == currencyId && x.RateType == rateType && x.IsActive && x.RateDate <= documentDate)
            .AddSort(nameof(ExchangeRate.RateDate), SortDirection.Descending)
            .ApplyPaging(0, 1);
        var found = await rates.ListAsync(spec, cancellationToken);
        var rate = found.FirstOrDefault()
            ?? throw new ConflictException("exchange_rate_not_found", $"No active exchange rate exists for '{currency.Code}' on or before {documentDate:yyyy-MM-dd}.");

        return new(currency.Id, currency.Code, currency.Symbol, currency.DecimalPlaces, rate.Rate, rate.RateDate, rate.RateType, ExchangeRateSource.System, false);
    }
}
