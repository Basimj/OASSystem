using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Identity.Entities;

namespace OAS.Tests.Features.Employees.Application;

public sealed class FakeUserRepository : IRepository<UserAccount, Guid>
{
    private readonly List<UserAccount> _users = [];

    public FakeUserRepository(params UserAccount[] users)
    {
        _users.AddRange(users);
    }

    public Task<UserAccount?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _users.FirstOrDefault(x => x.Id == id));
    }

    public Task<UserAccount?> GetForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _users.FirstOrDefault(x => x.Id == id));
    }

    public Task<IReadOnlyList<UserAccount>> ListAsync(
        ISpecification<UserAccount>? specification = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<UserAccount>>(
            _users.ToArray());
    }

    public Task<PagedData<UserAccount>> GetPageAsync(
        ISpecification<UserAccount> specification,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            new PagedData<UserAccount>(
                _users.ToArray(),
                _users.Count));
    }

    public Task<long> CountAsync(
        ISpecification<UserAccount>? specification = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)_users.Count);
    }

    public Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _users.Any(x => x.Id == id));
    }

    public Task AddAsync(
        UserAccount entity,
        CancellationToken cancellationToken = default)
    {
        _users.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(
        IEnumerable<UserAccount> entities,
        CancellationToken cancellationToken = default)
    {
        _users.AddRange(entities);
        return Task.CompletedTask;
    }

    public void Update(UserAccount entity)
    {
    }

    public void Delete(UserAccount entity)
    {
        _users.RemoveAll(x => x.Id == entity.Id);
    }

    public void DeleteRange(IEnumerable<UserAccount> entities)
    {
        foreach (var entity in entities)
            Delete(entity);
    }
}