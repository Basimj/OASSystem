using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.Settings;
public sealed record AccountingSettingsDto(Guid Id,Guid BaseCurrencyId,string BaseCurrencyCode,Guid? EmployeeParentAccountId,Guid? CashParentAccountId,Guid? BankParentAccountId,Guid? ExchangeGainAccountId,Guid? ExchangeLossAccountId,ExchangeRateType DefaultExchangeRateType,string RowVersion);
