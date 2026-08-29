using System.Linq.Expressions;

namespace OAS.Application.Abstractions.Persistence.Specifications;

public interface ISpecification<TEntity>
{
    Expression<Func<TEntity, bool>>? Criteria { get; }
    IReadOnlyList<SortDescriptor> Sorts { get; }
    int? Skip { get; }
    int? Take { get; }
    bool AsNoTracking { get; }
}
