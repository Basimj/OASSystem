using OAS.Domain.Sales.Enums;

namespace OAS.Application.Purchasing.Abstractions;

public sealed record PurchasingCommercialTerms(
    Guid CurrencyId,
    decimal ExchangeRate,
    DateOnly ExchangeRateDate,
    TaxCalculationMode TaxCalculationMode,
    int PaymentTermDays);

public interface IPurchasingCommercialTermsPort
{
    Task<PurchasingCommercialTerms> ResolveAsync(
        Guid supplierId,
        DateOnly documentDate,
        CancellationToken cancellationToken = default);
}
