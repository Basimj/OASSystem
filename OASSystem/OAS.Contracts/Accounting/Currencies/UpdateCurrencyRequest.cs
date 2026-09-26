namespace OAS.Contracts.Accounting.Currencies;
public sealed record UpdateCurrencyRequest(string NameAr,string? NameEn,string? Symbol,byte DecimalPlaces,bool IsActive,string RowVersion);
