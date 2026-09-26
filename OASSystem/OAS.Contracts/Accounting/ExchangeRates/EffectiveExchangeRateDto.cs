using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.ExchangeRates;
public sealed record EffectiveExchangeRateDto(Guid CurrencyId,string CurrencyCode,string? Symbol,byte DecimalPlaces,decimal Rate,DateOnly EffectiveRateDate,ExchangeRateType RateType,ExchangeRateSource Source,bool IsBaseCurrency);
