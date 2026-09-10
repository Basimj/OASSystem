using System.Linq.Expressions;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Tests.Features.Employees.Application;

public sealed class FakeEmployeeRepository : IRepository<Employee, Guid>
{
    private readonly List<Employee> _employees = [];

    public Employee? AddedEmployee { get; private set; }

    public FakeEmployeeRepository(params Employee[] employees)
    {
        _employees.AddRange(employees);
    }

    public Task<Employee?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _employees.FirstOrDefault(x => x.Id == id));
    }

    public Task<Employee?> GetForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _employees.FirstOrDefault(x => x.Id == id));
    }

    public Task<IReadOnlyList<Employee>> ListAsync(
        ISpecification<Employee>? specification = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Employee> query = ApplyCriteria(
            _employees,
            specification);

        return Task.FromResult<IReadOnlyList<Employee>>(
            query.ToArray());
    }

    public Task<PagedData<Employee>> GetPageAsync(
        ISpecification<Employee> specification,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Employee> query =
            ApplyCriteria(_employees, specification);

        query = ApplySorting(query, specification);

        var totalCount = query.LongCount();

        if (specification.Skip.HasValue)
            query = query.Skip(specification.Skip.Value);

        if (specification.Take.HasValue)
            query = query.Take(specification.Take.Value);

        return Task.FromResult(
            new PagedData<Employee>(
                query.ToArray(),
                totalCount));
    }

    public Task<long> CountAsync(
        ISpecification<Employee>? specification = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Employee> query =
            ApplyCriteria(_employees, specification);

        return Task.FromResult(query.LongCount());
    }

    public Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _employees.Any(x => x.Id == id));
    }

    public Task AddAsync(
        Employee entity,
        CancellationToken cancellationToken = default)
    {
        AddedEmployee = entity;
        _employees.Add(entity);

        return Task.CompletedTask;
    }

    public Task AddRangeAsync(
        IEnumerable<Employee> entities,
        CancellationToken cancellationToken = default)
    {
        _employees.AddRange(entities);
        return Task.CompletedTask;
    }

    public void Update(Employee entity)
    {
        var index = _employees.FindIndex(x => x.Id == entity.Id);

        if (index >= 0)
            _employees[index] = entity;
    }

    public void Delete(Employee entity)
    {
        _employees.RemoveAll(x => x.Id == entity.Id);
    }

    public void DeleteRange(IEnumerable<Employee> entities)
    {
        foreach (var entity in entities)
            Delete(entity);
    }

    private static IEnumerable<Employee> ApplyCriteria(
        IEnumerable<Employee> source,
        ISpecification<Employee>? specification)
    {
        if (specification?.Criteria is null)
            return source;

        return source.AsQueryable()
            .Where(specification.Criteria);
    }

    private static IEnumerable<Employee> ApplySorting(
        IEnumerable<Employee> source,
        ISpecification<Employee> specification)
    {
        IOrderedEnumerable<Employee>? ordered = null;

        foreach (var sort in specification.Sorts)
        {
            Func<Employee, object?> keySelector =
                sort.PropertyName switch
                {
                    "EmployeeNumber" => x => x.EmployeeNumber,
                    "EmployeeCode" => x => x.EmployeeNumber,
                    "FirstName" => x => x.FirstName,
                    "LastName" => x => x.LastName,
                    "IsActive" => x => x.IsActive,
                    "CreatedAtUtc" => x => x.CreatedAtUtc,
                    _ => x => x.EmployeeNumber
                };

            if (ordered is null)
            {
                ordered = sort.Direction ==
                          OAS.Contracts.Common.Pagination.SortDirection.Ascending
                    ? source.OrderBy(keySelector)
                    : source.OrderByDescending(keySelector);
            }
            else
            {
                ordered = sort.Direction ==
                          OAS.Contracts.Common.Pagination.SortDirection.Ascending
                    ? ordered.ThenBy(keySelector)
                    : ordered.ThenByDescending(keySelector);
            }
        }

        return ordered ?? source;
    }
}