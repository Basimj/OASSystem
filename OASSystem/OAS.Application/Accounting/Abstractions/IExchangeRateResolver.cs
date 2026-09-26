using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Abstractions;

public sealed record ExchangeRateResolution(
    Guid CurrencyId,
    string CurrencyCode,
    string? CurrencySymbol,
    byte CurrencyDecimalPlaces,
    decimal Rate,
    DateOnly RateDate,
    ExchangeRateType RateType,
    ExchangeRateSource Source,
    bool IsBaseCurrency);

public interface IExchangeRateResolver
{
    Task<ExchangeRateResolution> ResolveAsync(
        Guid currencyId,
        DateOnly documentDate,
        ExchangeRateType rateType = ExchangeRateType.Accounting,
        decimal? manualRate = null,
        bool manualOverrideAllowed = false,
        CancellationToken cancellationToken = default);
}
