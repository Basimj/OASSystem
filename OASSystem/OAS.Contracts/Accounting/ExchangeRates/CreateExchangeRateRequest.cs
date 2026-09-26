using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.ExchangeRates;
public sealed record CreateExchangeRateRequest(Guid CurrencyId,DateOnly RateDate,decimal Rate,ExchangeRateType RateType=ExchangeRateType.Accounting,bool IsActive=true);
