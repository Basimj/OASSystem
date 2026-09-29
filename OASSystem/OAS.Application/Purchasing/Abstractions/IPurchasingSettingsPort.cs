namespace OAS.Application.Purchasing.Abstractions;

public interface IPurchasingSettingsPort
{
    Task<PurchaseMatchingTolerances> GetMatchingTolerancesAsync(CancellationToken cancellationToken = default);
}
