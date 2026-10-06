using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.Settings;

public sealed record AccountingSettingsDto(
    Guid Id,
    Guid BaseCurrencyId,
    string BaseCurrencyCode,
    Guid? EmployeeParentAccountId,
    Guid? CashParentAccountId,
    Guid? BankParentAccountId,
    Guid? ExchangeGainAccountId,
    Guid? ExchangeLossAccountId,
    Guid? SalesRevenueAccountId,
    Guid? TaxPayableAccountId,
    Guid? InventoryAccountId,
    Guid? CogsAccountId,
    Guid? GrniAccountId,
    Guid? PurchaseTaxAccountId,
    Guid? PurchasePriceVarianceAccountId,
    ExchangeRateType DefaultExchangeRateType,
    string RowVersion,
    Guid? RetainedEarningsAccountId);
