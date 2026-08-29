using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Common.Entities;
using OAS.Domain.Common.Interfaces;

namespace OAS.Infrastructure.Persistence.Repositories.Generic;

public class EfRepository<TEntity, TKey>(OasDbContext dbContext) : IRepository<TEntity, TKey>
    where TEntity : Entity<TKey> where TKey : notnull
{
    protected OasDbContext DbContext { get; } = dbContext;
    protected DbSet<TEntity> Set => DbContext.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default) =>
        await ApplySoftDeleteFilter(Set.AsNoTracking()).FirstOrDefaultAsync(BuildIdPredicate(id), cancellationToken);

    public async Task<TEntity?> GetForUpdateAsync(TKey id, CancellationToken cancellationToken = default) =>
        await ApplySoftDeleteFilter(Set).FirstOrDefaultAsync(BuildIdPredicate(id), cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity>? specification = null, CancellationToken cancellationToken = default) =>
        await ApplySpecification(ApplySoftDeleteFilter(Set.AsQueryable()), specification).ToListAsync(cancellationToken);

    public async Task<PagedData<TEntity>> GetPageAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var baseQuery = ApplySoftDeleteFilter(Set.AsNoTracking());
        if (specification.Criteria is not null) baseQuery = baseQuery.Where(specification.Criteria);
        var count = await baseQuery.LongCountAsync(cancellationToken);
        var dataQuery = ApplyOrderingAndPaging(baseQuery, specification);
        var items = await dataQuery.ToListAsync(cancellationToken);
        return new PagedData<TEntity>(items, count);
    }

    public Task<long> CountAsync(ISpecification<TEntity>? specification = null, CancellationToken cancellationToken = default)
    {
        var query = ApplySoftDeleteFilter(Set.AsNoTracking());
        if (specification?.Criteria is not null) query = query.Where(specification.Criteria);
        return query.LongCountAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default) =>
        ApplySoftDeleteFilter(Set.AsNoTracking()).AnyAsync(BuildIdPredicate(id), cancellationToken);

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) => Set.AddAsync(entity, cancellationToken).AsTask();
    public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => Set.AddRangeAsync(entities, cancellationToken);
    public void Update(TEntity entity) => Set.Update(entity);
    public void Delete(TEntity entity) => Set.Remove(entity);
    public void DeleteRange(IEnumerable<TEntity> entities) => Set.RemoveRange(entities);

    private static IQueryable<TEntity> ApplySoftDeleteFilter(IQueryable<TEntity> query)
    {
        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity)))
            query = query.Where(entity => !EF.Property<bool>(entity, nameof(ISoftDeletable.IsDeleted)));
        return query;
    }

    private static IQueryable<TEntity> ApplySpecification(IQueryable<TEntity> query, ISpecification<TEntity>? specification)
    {
        if (specification is null) return query;
        if (specification.AsNoTracking) query = query.AsNoTracking();
        if (specification.Criteria is not null) query = query.Where(specification.Criteria);
        return ApplyOrderingAndPaging(query, specification);
    }

    private static IQueryable<TEntity> ApplyOrderingAndPaging(IQueryable<TEntity> query, ISpecification<TEntity> specification)
    {
        var ordered = query;
        var firstSort = true;
        foreach (var sort in specification.Sorts)
        {
            ordered = ApplyOrderBy(ordered, sort.PropertyName, sort.Direction, firstSort);
            firstSort = false;
        }
        if (specification.Skip is int skip) ordered = ordered.Skip(skip);
        if (specification.Take is int take) ordered = ordered.Take(take);
        return ordered;
    }

    private static IQueryable<TEntity> ApplyOrderBy(IQueryable<TEntity> source, string propertyName, OAS.Contracts.Common.Pagination.SortDirection direction, bool first)
    {
        var property = typeof(TEntity).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?? throw new InvalidOperationException($"Unknown sort property '{propertyName}' for {typeof(TEntity).Name}.");
        if (property.GetIndexParameters().Length != 0)
            throw new InvalidOperationException($"Indexed property '{propertyName}' cannot be used for sorting.");
        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var selector = Expression.Lambda(Expression.Property(parameter, property), parameter);
        var methodName = first
            ? (direction == OAS.Contracts.Common.Pagination.SortDirection.Descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy))
            : (direction == OAS.Contracts.Common.Pagination.SortDirection.Descending ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy));
        var call = Expression.Call(typeof(Queryable), methodName, [typeof(TEntity), property.PropertyType], source.Expression, Expression.Quote(selector));
        return source.Provider.CreateQuery<TEntity>(call);
    }

    private static Expression<Func<TEntity, bool>> BuildIdPredicate(TKey id)
    {
        var entity = Expression.Parameter(typeof(TEntity), "entity");
        var idProperty = Expression.Property(entity, nameof(Entity<TKey>.Id));
        var idValue = Expression.Constant(id, typeof(TKey));
        return Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(idProperty, idValue), entity);
    }
}
