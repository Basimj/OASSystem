using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.ReceiptVouchers;
public sealed record CreateReceiptVoucherLineRequest(
    SettlementPartyType PartyType,
    Guid? CustomerId,
    Guid? SupplierId,
    Guid? EmployeeId,
    string? PartyName,
    Guid? CounterpartyAccountId,
    PaymentMethod PaymentMethod,
    Guid? CashAccountId,
    Guid? BankAccountId,
    Guid? SettlementAccountId,
    Guid CurrencyId,
    decimal Amount,
    decimal? ExchangeRate = null,
    ExchangeRateType ExchangeRateType = ExchangeRateType.Accounting,
    string? ReferenceNumber = null,
    DateOnly? ReferenceDate = null,
    string? ReferenceType = null,
    Guid? ReferenceId = null,
    string? Description = null);
