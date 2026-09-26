namespace OAS.Contracts.Accounting.Currencies;
public sealed record CurrencyDto(Guid Id,string Code,string NameAr,string? NameEn,string? Symbol,byte DecimalPlaces,bool IsActive,string RowVersion);
