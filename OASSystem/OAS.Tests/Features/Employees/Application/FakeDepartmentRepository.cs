using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Tests.Features.Employees.Application;

public sealed class FakeDepartmentRepository : IRepository<Department, Guid>
{
    private readonly List<Department> _items;

    public FakeDepartmentRepository(params Department[] items) => _items = [.. items];

    public Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

    public Task<Department?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Department>> ListAsync(
        ISpecification<Department>? specification = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Department> query = _items;
        if (specification?.Criteria is not null)
            query = query.AsQueryable().Where(specification.Criteria);
        return Task.FromResult<IReadOnlyList<Department>>(query.ToArray());
    }

    public Task<PagedData<Department>> GetPageAsync(
        ISpecification<Department> specification,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedData<Department>(_items.ToArray(), _items.Count));

    public async Task<long> CountAsync(
        ISpecification<Department>? specification = null,
        CancellationToken cancellationToken = default) =>
        (await ListAsync(specification, cancellationToken)).LongCount();

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Any(x => x.Id == id));

    public Task AddAsync(Department entity, CancellationToken cancellationToken = default)
    {
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Department> entities, CancellationToken cancellationToken = default)
    {
        _items.AddRange(entities);
        return Task.CompletedTask;
    }

    public void Update(Department entity) { }
    public void Delete(Department entity) => _items.Remove(entity);
    public void DeleteRange(IEnumerable<Department> entities)
    {
        foreach (var entity in entities.ToArray()) _items.Remove(entity);
    }
}
