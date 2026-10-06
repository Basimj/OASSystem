using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.Settings;

public sealed record UpdateAccountingSettingsRequest(
    Guid BaseCurrencyId,
    Guid? EmployeeParentAccountId,
    Guid? CashParentAccountId,
    Guid? BankParentAccountId,
    Guid? ExchangeGainAccountId,
    Guid? ExchangeLossAccountId,
    Guid? SalesRevenueAccountId,
    Guid? TaxPayableAccountId,
    Guid? InventoryAccountId,
    Guid? CogsAccountId,
    ExchangeRateType DefaultExchangeRateType = ExchangeRateType.Accounting,
    string? RowVersion = null,
    Guid? RetainedEarningsAccountId = null,
    Guid? GrniAccountId = null,
    Guid? PurchaseTaxAccountId = null,
    Guid? PurchasePriceVarianceAccountId = null);
