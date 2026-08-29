using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Common.Entities;

namespace OAS.Application.Abstractions.Persistence;

public interface IReadRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity>? specification = null, CancellationToken cancellationToken = default);
    Task<PagedData<TEntity>> GetPageAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<long> CountAsync(ISpecification<TEntity>? specification = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
}
