using OAS.Application.Abstractions.Persistence;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.Abstractions;

public interface ISupplierCatalogRepository
{
    Task<SupplierCatalogItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupplierCatalogItem?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedData<SupplierCatalogItem>> GetPageAsync(PageRequest request, Guid? supplierId, Guid? productVariantId, bool? isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierPriceHistory>> GetPricesAsync(Guid catalogItemId, CancellationToken cancellationToken = default);
    Task<PurchasingCatalogDefaults?> GetPurchaseDefaultsAsync(Guid supplierId, Guid productVariantId, Guid currencyId, CancellationToken cancellationToken = default);
    Task<SupplierPriceHistory?> GetPriceForUpdateAsync(Guid priceId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid supplierId, Guid productVariantId, Guid purchaseUnitId, Guid? exceptId, CancellationToken cancellationToken = default);
    Task<bool> HasCurrentPriceAsync(Guid catalogItemId, Guid currencyId, Guid? exceptPriceId, CancellationToken cancellationToken = default);
    Task AddAsync(SupplierCatalogItem item, CancellationToken cancellationToken = default);
    Task AddPriceAsync(SupplierPriceHistory price, CancellationToken cancellationToken = default);
    void Update(SupplierCatalogItem item);
    void UpdatePrice(SupplierPriceHistory price);
}
