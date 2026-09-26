using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.Settings;
public sealed record UpdateAccountingSettingsRequest(Guid BaseCurrencyId,Guid? EmployeeParentAccountId,Guid? CashParentAccountId,Guid? BankParentAccountId,Guid? ExchangeGainAccountId,Guid? ExchangeLossAccountId,ExchangeRateType DefaultExchangeRateType=ExchangeRateType.Accounting,string? RowVersion=null);
