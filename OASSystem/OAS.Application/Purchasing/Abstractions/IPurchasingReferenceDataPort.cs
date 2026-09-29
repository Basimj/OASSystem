namespace OAS.Application.Purchasing.Abstractions;

public interface IPurchasingReferenceDataPort
{
    Task<PurchasingSupplierSnapshot?> GetSupplierAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchasingWarehouseSnapshot?> GetWarehouseAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchasingUnitSnapshot?> GetUnitAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchasingCurrencySnapshot?> GetCurrencyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchasingProductSnapshot?> GetProductVariantAsync(Guid id, CancellationToken cancellationToken = default);
}
