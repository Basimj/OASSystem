using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Common.Entities;

namespace OAS.Tests.Accounting.Application.Common;

public class FakeRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    private readonly List<TEntity> _items = [];

    public IReadOnlyList<TEntity> Items => _items.AsReadOnly();
    public TEntity? LastAdded { get; private set; }
    public TEntity? LastUpdated { get; private set; }
    public TEntity? LastDeleted { get; private set; }

    public FakeRepository(IEnumerable<TEntity>? initialItems = null)
    {
        if (initialItems != null)
        {
            _items.AddRange(initialItems);
        }
    }

    public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.FirstOrDefault(x => EqualityComparer<TKey>.Default.Equals(x.Id, id)));
    }

    public Task<TEntity?> GetForUpdateAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity>? specification = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<TEntity> query = ApplyCriteria(_items, specification);
        return Task.FromResult<IReadOnlyList<TEntity>>(query.ToList());
    }

    public Task<PagedData<TEntity>> GetPageAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        IEnumerable<TEntity> query = ApplyCriteria(_items, specification);

        var total = query.LongCount();

        if (specification.Skip.HasValue)
            query = query.Skip(specification.Skip.Value);

        if (specification.Take.HasValue)
            query = query.Take(specification.Take.Value);

        return Task.FromResult(new PagedData<TEntity>(query.ToList(), total));
    }

    public Task<long> CountAsync(ISpecification<TEntity>? specification = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<TEntity> query = ApplyCriteria(_items, specification);
        return Task.FromResult(query.LongCount());
    }

    public Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.Any(x => EqualityComparer<TKey>.Default.Equals(x.Id, id)));
    }

    public Task<bool> ExistsAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        IEnumerable<TEntity> query = ApplyCriteria(_items, specification);
        return Task.FromResult(query.Any());
    }

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _items.Add(entity);
        LastAdded = entity;
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        _items.AddRange(entities);
        return Task.CompletedTask;
    }

    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        LastUpdated = entity;
        var index = _items.FindIndex(x => EqualityComparer<TKey>.Default.Equals(x.Id, entity.Id));
        if (index >= 0)
        {
            _items[index] = entity;
        }
        else
        {
            _items.Add(entity);
        }
    }

    public void Delete(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        LastDeleted = entity;
        _items.RemoveAll(x => EqualityComparer<TKey>.Default.Equals(x.Id, entity.Id));
    }

    public void DeleteRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var toRemove = entities.Select(e => e.Id).ToHashSet();
        _items.RemoveAll(x => toRemove.Contains(x.Id));
    }

    private static IEnumerable<TEntity> ApplyCriteria(IEnumerable<TEntity> source, ISpecification<TEntity>? specification)
    {
        if (specification?.Criteria == null)
            return source;

        return source.AsQueryable().Where(specification.Criteria);
    }
}
