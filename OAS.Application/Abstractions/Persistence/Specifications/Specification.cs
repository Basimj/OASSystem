using System.Linq.Expressions;

namespace OAS.Application.Abstractions.Persistence.Specifications;

public class Specification<TEntity> : ISpecification<TEntity>
{
    private readonly List<SortDescriptor> _sorts = [];

    public Expression<Func<TEntity, bool>>? Criteria { get; private set; }
    public IReadOnlyList<SortDescriptor> Sorts => _sorts;
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool AsNoTracking { get; private set; } = true;

    public Specification<TEntity> Where(Expression<Func<TEntity, bool>> criteria)
    {
        Criteria = criteria;
        return this;
    }

    public Specification<TEntity> AddSort(string propertyName, OAS.Contracts.Common.Pagination.SortDirection direction)
    {
        if (!string.IsNullOrWhiteSpace(propertyName)) _sorts.Add(new SortDescriptor(propertyName.Trim(), direction));
        return this;
    }

    public Specification<TEntity> ApplyPaging(int skip, int take)
    {
        Skip = Math.Max(0, skip);
        Take = Math.Max(1, take);
        return this;
    }

    public Specification<TEntity> Tracking(bool enabled = true)
    {
        AsNoTracking = !enabled;
        return this;
    }
}
