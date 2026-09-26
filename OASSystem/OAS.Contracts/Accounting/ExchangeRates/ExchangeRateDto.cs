using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.ExchangeRates;
public sealed record ExchangeRateDto(Guid Id,Guid CurrencyId,string CurrencyCode,DateOnly RateDate,decimal Rate,ExchangeRateType RateType,bool IsActive,string RowVersion);
