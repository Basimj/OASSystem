using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Tests.Features.Employees.Application;

public sealed class FakeJobTitleRepository : IRepository<JobTitle, Guid>
{
    private readonly List<JobTitle> _items;
    public JobTitle? AddedItem { get; private set; }

    public FakeJobTitleRepository(params JobTitle[] items) => _items = [.. items];

    public Task<JobTitle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

    public Task<JobTitle?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<JobTitle>> ListAsync(ISpecification<JobTitle>? specification = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<JobTitle> query = _items;
        if (specification?.Criteria is not null)
            query = query.AsQueryable().Where(specification.Criteria);
        return Task.FromResult<IReadOnlyList<JobTitle>>(query.ToArray());
    }

    public Task<PagedData<JobTitle>> GetPageAsync(ISpecification<JobTitle> specification, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedData<JobTitle>(_items.ToArray(), _items.Count));

    public async Task<long> CountAsync(ISpecification<JobTitle>? specification = null, CancellationToken cancellationToken = default) =>
        (await ListAsync(specification, cancellationToken)).LongCount();

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Any(x => x.Id == id));

    public Task AddAsync(JobTitle entity, CancellationToken cancellationToken = default)
    {
        AddedItem = entity;
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<JobTitle> entities, CancellationToken cancellationToken = default)
    {
        _items.AddRange(entities);
        return Task.CompletedTask;
    }

    public void Update(JobTitle entity) { }
    public void Delete(JobTitle entity) => _items.Remove(entity);
    public void DeleteRange(IEnumerable<JobTitle> entities) { foreach (var entity in entities.ToArray()) _items.Remove(entity); }
}
