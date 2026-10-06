using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Purchasing.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Purchasing.Entities;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Purchasing.Persistence.Repositories;

public sealed class SupplierCatalogRepository(OasDbContext dbContext) : ISupplierCatalogRepository
{
    private DbSet<SupplierCatalogItem> Items => dbContext.Set<SupplierCatalogItem>();
    private DbSet<SupplierPriceHistory> Prices => dbContext.Set<SupplierPriceHistory>();

    public Task<SupplierCatalogItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<SupplierCatalogItem?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        Items.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PagedData<SupplierCatalogItem>> GetPageAsync(PageRequest request, Guid? supplierId, Guid? productVariantId, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = Items.AsNoTracking().AsQueryable();
        if (supplierId.HasValue) query = query.Where(x => x.SupplierId == supplierId.Value);
        if (productVariantId.HasValue) query = query.Where(x => x.ProductVariantId == productVariantId.Value);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        query = query.OrderBy(x => x.SupplierProductName).ThenBy(x => x.SupplierProductCode);
        return PurchasingRepositoryHelpers.PageAsync(query, request, (q, term) => q.Where(x =>
            (x.SupplierProductCode != null && x.SupplierProductCode.Contains(term)) ||
            (x.SupplierProductName != null && x.SupplierProductName.Contains(term))), cancellationToken);
    }

    public async Task<IReadOnlyList<SupplierPriceHistory>> GetPricesAsync(Guid catalogItemId, CancellationToken cancellationToken = default) =>
        await Prices.AsNoTracking().Where(x => x.SupplierCatalogItemId == catalogItemId)
            .OrderByDescending(x => x.EffectiveFrom).ToListAsync(cancellationToken);

    public async Task<PurchasingCatalogDefaults?> GetPurchaseDefaultsAsync(Guid supplierId, Guid productVariantId, Guid currencyId, CancellationToken cancellationToken = default)
    {
        // Prefer an active supplier-catalog row that has a current price in the requested currency.
        var priced = await (from item in Items.AsNoTracking()
                            join price in Prices.AsNoTracking() on item.Id equals price.SupplierCatalogItemId
                            where item.SupplierId == supplierId && item.ProductVariantId == productVariantId && item.IsActive
                                  && price.CurrencyId == currencyId && price.IsCurrent
                            orderby item.IsPreferred descending, price.EffectiveFrom descending
                            select new { item.Id, item.PurchaseUnitId, item.UnitConversionFactor, price.UnitPrice })
            .FirstOrDefaultAsync(cancellationToken);

        if (priced is not null)
            return new PurchasingCatalogDefaults(priced.Id, priced.PurchaseUnitId, priced.UnitConversionFactor, priced.UnitPrice);

        // A catalog entry without a current price must not block creation of a PO draft.
        // Keep the supplier's purchase unit/conversion and let the user enter the commercial price on the draft.
        var catalog = await Items.AsNoTracking()
            .Where(x => x.SupplierId == supplierId && x.ProductVariantId == productVariantId && x.IsActive)
            .OrderByDescending(x => x.IsPreferred)
            .ThenBy(x => x.SupplierProductName)
            .Select(x => new { x.Id, x.PurchaseUnitId, x.UnitConversionFactor })
            .FirstOrDefaultAsync(cancellationToken);

        return catalog is null
            ? null
            : new PurchasingCatalogDefaults(catalog.Id, catalog.PurchaseUnitId, catalog.UnitConversionFactor, null);
    }

    public Task<SupplierPriceHistory?> GetPriceForUpdateAsync(Guid priceId, CancellationToken cancellationToken = default) =>
        Prices.FirstOrDefaultAsync(x => x.Id == priceId, cancellationToken);

    public Task<bool> ExistsAsync(Guid supplierId, Guid productVariantId, Guid purchaseUnitId, Guid? exceptId, CancellationToken cancellationToken = default) =>
        Items.AsNoTracking().AnyAsync(x => x.SupplierId == supplierId && x.ProductVariantId == productVariantId && x.PurchaseUnitId == purchaseUnitId && (!exceptId.HasValue || x.Id != exceptId.Value), cancellationToken);

    public Task<bool> HasCurrentPriceAsync(Guid catalogItemId, Guid currencyId, Guid? exceptPriceId, CancellationToken cancellationToken = default) =>
        Prices.AsNoTracking().AnyAsync(x => x.SupplierCatalogItemId == catalogItemId && x.CurrencyId == currencyId && x.IsCurrent && (!exceptPriceId.HasValue || x.Id != exceptPriceId.Value), cancellationToken);

    public Task AddAsync(SupplierCatalogItem item, CancellationToken cancellationToken = default) => Items.AddAsync(item, cancellationToken).AsTask();
    public Task AddPriceAsync(SupplierPriceHistory price, CancellationToken cancellationToken = default) => Prices.AddAsync(price, cancellationToken).AsTask();
    public void Update(SupplierCatalogItem item) { if (dbContext.Entry(item).State == EntityState.Detached) { Items.Attach(item); dbContext.Entry(item).State = EntityState.Modified; } }
    public void UpdatePrice(SupplierPriceHistory price) { if (dbContext.Entry(price).State == EntityState.Detached) { Prices.Attach(price); dbContext.Entry(price).State = EntityState.Modified; } }
}
