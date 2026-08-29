using System.Linq.Expressions;
using System.Reflection;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;

namespace OAS.Application.CRUD.Specifications;

public sealed class ConventionCrudSpecificationFactory<TEntity> : ICrudSpecificationFactory<TEntity>
{
    public ISpecification<TEntity> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var spec = new Specification<TEntity>();

        if (normalized.Search is { Length: > 0 } search)
        {
            var predicate = BuildSearchPredicate(search);
            if (predicate is not null) spec.Where(predicate);
        }

        var sortProperty = ResolveSortProperty(normalized.SortBy) ?? "Id";
        spec.AddSort(sortProperty, normalized.SortDirection);
        spec.ApplyPaging((normalized.PageNumber - 1) * normalized.PageSize, normalized.PageSize);
        return spec;
    }

    private static string? ResolveSortProperty(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested)) return null;
        return typeof(TEntity).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(p => p.CanRead && p.GetIndexParameters().Length == 0 && IsSortableType(p.PropertyType) && string.Equals(p.Name, requested, StringComparison.OrdinalIgnoreCase))?.Name;
    }

    private static Expression<Func<TEntity, bool>>? BuildSearchPredicate(string search)
    {
        var stringProperties = typeof(TEntity).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && p.PropertyType == typeof(string))
            .ToArray();
        if (stringProperties.Length == 0) return null;

        var entity = Expression.Parameter(typeof(TEntity), "entity");
        var searchValue = Expression.Constant(search);
        var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        Expression? body = null;

        foreach (var property in stringProperties)
        {
            var member = Expression.Property(entity, property);
            var notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
            var containsCall = Expression.Call(member, contains, searchValue);
            var clause = Expression.AndAlso(notNull, containsCall);
            body = body is null ? clause : Expression.OrElse(body, clause);
        }
        return body is null ? null : Expression.Lambda<Func<TEntity, bool>>(body, entity);
    }

    private static bool IsSortableType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Guid) ||
               type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) || type == typeof(DateOnly) || type == typeof(TimeOnly);
    }
}
