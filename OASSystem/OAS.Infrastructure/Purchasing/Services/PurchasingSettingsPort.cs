using Microsoft.Extensions.Configuration;
using OAS.Application.Purchasing.Abstractions;

namespace OAS.Infrastructure.Purchasing.Services;

public sealed class PurchasingSettingsPort(IConfiguration configuration) : IPurchasingSettingsPort
{
    public Task<PurchaseMatchingTolerances> GetMatchingTolerancesAsync(CancellationToken cancellationToken = default)
    {
        var quantity = Math.Max(0m, configuration.GetValue<decimal?>("Purchasing:Matching:QuantityTolerance") ?? 0m);
        var price = Math.Max(0m, configuration.GetValue<decimal?>("Purchasing:Matching:PriceTolerance") ?? 0m);
        var tax = Math.Max(0m, configuration.GetValue<decimal?>("Purchasing:Matching:TaxTolerance") ?? 0m);
        return Task.FromResult(new PurchaseMatchingTolerances(quantity, price, tax));
    }
}
