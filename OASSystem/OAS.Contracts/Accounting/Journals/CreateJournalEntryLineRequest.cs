using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.Journals;
public sealed record CreateJournalEntryLineRequest(
    Guid AccountId,
    decimal TransactionDebitAmount,
    decimal TransactionCreditAmount,
    Guid? TransactionCurrencyId = null,
    decimal? ExchangeRate = null,
    ExchangeRateType ExchangeRateType = ExchangeRateType.Accounting,
    string? Description = null,
    Guid? CustomerId = null,
    Guid? SupplierId = null,
    Guid? EmployeeId = null,
    Guid? CostCenterId = null,
    Guid? ProductVariantId = null,
    Guid? WarehouseId = null);
