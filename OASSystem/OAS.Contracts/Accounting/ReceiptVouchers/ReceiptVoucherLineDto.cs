using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.ReceiptVouchers;
public sealed record ReceiptVoucherLineDto(
    Guid Id,Guid ReceiptVoucherId,int LineNumber,Guid AccountId,
    SettlementPartyType? PartyType,Guid? CustomerId,Guid? SupplierId,Guid? EmployeeId,string? PartyNameSnapshot,Guid? CounterpartyAccountId,
    PaymentMethod? PaymentMethod,Guid? CashAccountId,Guid? BankAccountId,Guid? SettlementAccountId,
    Guid? CurrencyId,string? CurrencyCodeSnapshot,string? CurrencySymbolSnapshot,byte? CurrencyDecimalPlacesSnapshot,
    decimal Amount,decimal? ExchangeRate,DateOnly? ExchangeRateDate,ExchangeRateType? ExchangeRateType,ExchangeRateSource? ExchangeRateSource,decimal? BaseAmount,
    string? ReferenceNumber,DateOnly? ReferenceDate,string? ReferenceType,Guid? ReferenceId,string? Description);
