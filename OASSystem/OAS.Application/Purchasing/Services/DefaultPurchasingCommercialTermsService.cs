using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Purchasing.Services;

/// <summary>
/// V1 resolver for customer-demand PO creation. It deliberately resolves through the
/// accounting base currency/exchange-rate service instead of hard-coding a currency id.
/// Supplier-specific commercial terms can replace this port later without changing the use case.
/// </summary>
public sealed class DefaultPurchasingCommercialTermsService(
    IReadRepository<AccountingSettings, Guid> settings,
    IExchangeRateResolver exchangeRates) : IPurchasingCommercialTermsPort
{
    public async Task<PurchasingCommercialTerms> ResolveAsync(
        Guid supplierId,
        DateOnly documentDate,
        CancellationToken cancellationToken = default)
    {
        var accounting = await settings.GetByIdAsync(AccountingSettings.SingletonId, cancellationToken)
            ?? throw new ConflictException("accounting_settings_required", "يجب إعداد المحاسبة والعملة الأساسية أولًا.");
        var rate = await exchangeRates.ResolveAsync(
            accounting.BaseCurrencyId,
            documentDate,
            ExchangeRateType.Accounting,
            cancellationToken: cancellationToken);
        return new PurchasingCommercialTerms(
            accounting.BaseCurrencyId,
            rate.Rate,
            rate.RateDate,
            TaxCalculationMode.Exclusive,
            0);
    }
}
