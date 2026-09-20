using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Inventory.Repositories;
using OAS.Domain.Entities.Inventory;

namespace OAS.Tests.Inventory.Fakes;

public sealed class FakeInventoryBalanceRepository : IInventoryBalanceRepository
{
    private readonly List<InventoryBalance> _items = [];

    public List<InventoryBalance> Items => _items;

    public Task<InventoryBalance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

    public Task<InventoryBalance?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public Task<InventoryBalance?> GetByWarehouseAndVariantAsync(
        Guid warehouseId,
        Guid productVariantId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(x => x.WarehouseId == warehouseId && x.ProductVariantId == productVariantId));

    public Task<IReadOnlyList<InventoryBalance>> GetByWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<InventoryBalance>>(_items.Where(x => x.WarehouseId == warehouseId).ToList());

    public Task<IReadOnlyList<InventoryBalance>> ListAsync(
        ISpecification<InventoryBalance>? specification = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<InventoryBalance>>(_items.ToList());

    public Task<PagedData<InventoryBalance>> GetPageAsync(
        ISpecification<InventoryBalance> specification,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedData<InventoryBalance>(_items, _items.Count));

    public Task<long> CountAsync(
        ISpecification<InventoryBalance>? specification = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult((long)_items.Count);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Any(x => x.Id == id));

    public Task AddAsync(InventoryBalance entity, CancellationToken cancellationToken = default)
    {
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<InventoryBalance> entities, CancellationToken cancellationToken = default)
    {
        _items.AddRange(entities);
        return Task.CompletedTask;
    }

    public void Update(InventoryBalance entity)
    {
        var idx = _items.FindIndex(x => x.Id == entity.Id);
        if (idx >= 0) _items[idx] = entity;
    }

    public void Delete(InventoryBalance entity) => _items.RemoveAll(x => x.Id == entity.Id);
    public void DeleteRange(IEnumerable<InventoryBalance> entities)
    {
        var ids = entities.Select(e => e.Id).ToHashSet();
        _items.RemoveAll(x => ids.Contains(x.Id));
    }
}
