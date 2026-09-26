using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.Journals;
public sealed record JournalEntryLineDto(
    Guid Id,Guid JournalEntryId,int LineNumber,Guid AccountId,
    decimal DebitAmount,decimal CreditAmount,
    Guid? TransactionCurrencyId,string? TransactionCurrencyCodeSnapshot,byte? TransactionCurrencyDecimalPlacesSnapshot,
    decimal? TransactionDebitAmount,decimal? TransactionCreditAmount,decimal? ExchangeRate,DateOnly? ExchangeRateDate,
    ExchangeRateType? ExchangeRateType,ExchangeRateSource? ExchangeRateSource,Guid? SourceDocumentLineId,
    string? Description,Guid? CustomerId,Guid? SupplierId,Guid? EmployeeId,string? PartyNameSnapshot,
    Guid? CostCenterId,Guid? ProductVariantId,Guid? WarehouseId);
