namespace OAS.Contracts.Accounting.Currencies;
public sealed record CreateCurrencyRequest(string Code,string NameAr,string? NameEn,string? Symbol,byte DecimalPlaces,bool IsActive=true);
