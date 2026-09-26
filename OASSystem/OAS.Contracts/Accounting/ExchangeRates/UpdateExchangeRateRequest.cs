using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.ExchangeRates;
public sealed record UpdateExchangeRateRequest(DateOnly RateDate,decimal Rate,ExchangeRateType RateType,bool IsActive,string RowVersion);
