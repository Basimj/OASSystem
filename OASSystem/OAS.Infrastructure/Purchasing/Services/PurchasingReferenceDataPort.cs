using Microsoft.EntityFrameworkCore;
using OAS.Application.Purchasing.Abstractions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Purchasing.Services;

public sealed class PurchasingReferenceDataPort(OasDbContext dbContext) : IPurchasingReferenceDataPort
{
    public async Task<PurchasingSupplierSnapshot?> GetSupplierAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var value = await dbContext.Set<Supplier>().AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.SupplierCode, x.NameAr, x.IsActive, x.AccountId }).FirstOrDefaultAsync(cancellationToken);
        return value is null ? null : new PurchasingSupplierSnapshot(value.Id, value.SupplierCode, value.NameAr, value.IsActive, value.AccountId);
    }

    public async Task<PurchasingWarehouseSnapshot?> GetWarehouseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var value = await dbContext.Set<Warehouse>().AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Code, x.NameAr, x.IsActive }).FirstOrDefaultAsync(cancellationToken);
        return value is null ? null : new PurchasingWarehouseSnapshot(value.Id, value.Code, value.NameAr, value.IsActive);
    }

    public async Task<PurchasingUnitSnapshot?> GetUnitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var value = await dbContext.Set<Unit>().AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Code, x.NameAr, x.IsActive }).FirstOrDefaultAsync(cancellationToken);
        return value is null ? null : new PurchasingUnitSnapshot(value.Id, value.Code, value.NameAr, value.IsActive);
    }

    public async Task<PurchasingCurrencySnapshot?> GetCurrencyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var value = await dbContext.Set<Currency>().AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Code, x.IsActive }).FirstOrDefaultAsync(cancellationToken);
        return value is null ? null : new PurchasingCurrencySnapshot(value.Id, value.Code, value.IsActive);
    }

    public async Task<PurchasingProductSnapshot?> GetProductVariantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var value = await (from variant in dbContext.Set<ProductVariant>().AsNoTracking()
                           join product in dbContext.Set<Product>().AsNoTracking() on variant.ProductId equals product.Id
                           where variant.Id == id
                           select new
                           {
                               VariantId = variant.Id,
                               ProductCode = product.ProductCode,
                               ProductName = product.NameAr,
                               VariantIsActive = variant.IsActive,
                               ProductIsActive = product.IsActive,
                               product.IsStockItem,
                               DefaultUnitId = variant.UnitId
                           }).FirstOrDefaultAsync(cancellationToken);
        return value is null ? null : new PurchasingProductSnapshot(value.VariantId, value.ProductCode, value.ProductName,
            value.VariantIsActive, value.ProductIsActive, value.IsStockItem, value.DefaultUnitId);
    }
}
