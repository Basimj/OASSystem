using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Common.Entities;

namespace OAS.Tests.Inventory.Fakes;

public class FakeGenericRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    protected readonly List<TEntity> _items;

    public FakeGenericRepository(List<TEntity>? items = null)
    {
        _items = items ?? [];
    }

    public List<TEntity> Items => _items;

    public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(x => EqualityComparer<TKey>.Default.Equals(x.Id, id)));

    public Task<TEntity?> GetForUpdateAsync(TKey id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity>? specification = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TEntity>>(Apply(specification, applyPaging: true).ToList());

    public Task<PagedData<TEntity>> GetPageAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var filtered = Apply(specification, applyPaging: false).ToList();
        var paged = ApplyPaging(filtered, specification).ToList();
        return Task.FromResult(new PagedData<TEntity>(paged, filtered.Count));
    }

    public Task<long> CountAsync(
        ISpecification<TEntity>? specification = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Apply(specification, applyPaging: false).LongCount());

    public Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Any(x => EqualityComparer<TKey>.Default.Equals(x.Id, id)));

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        _items.AddRange(entities);
        return Task.CompletedTask;
    }

    public void Update(TEntity entity)
    {
        var idx = _items.FindIndex(x => EqualityComparer<TKey>.Default.Equals(x.Id, entity.Id));
        if (idx >= 0) _items[idx] = entity;
    }

    public void Delete(TEntity entity) =>
        _items.RemoveAll(x => EqualityComparer<TKey>.Default.Equals(x.Id, entity.Id));

    public void DeleteRange(IEnumerable<TEntity> entities)
    {
        var ids = entities.Select(e => e.Id).ToHashSet();
        _items.RemoveAll(x => ids.Contains(x.Id));
    }

    private IEnumerable<TEntity> Apply(ISpecification<TEntity>? specification, bool applyPaging)
    {
        IEnumerable<TEntity> query = _items;
        if (specification?.Criteria is not null)
            query = query.Where(specification.Criteria.Compile());

        return applyPaging && specification is not null
            ? ApplyPaging(query, specification)
            : query;
    }

    private static IEnumerable<TEntity> ApplyPaging(IEnumerable<TEntity> query, ISpecification<TEntity> specification)
    {
        if (specification.Skip.HasValue)
            query = query.Skip(specification.Skip.Value);
        if (specification.Take.HasValue)
            query = query.Take(specification.Take.Value);
        return query;
    }
}
