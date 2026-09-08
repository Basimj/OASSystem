using System.Linq.Expressions;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Specifications;

public sealed class EmployeePageSpecification
    : Specification<Employee>
{
    private static readonly string[] AllowedSorts =
    [
        "EmployeeCode",
        "FirstName",
        "LastName",
        "JobTitle",
        "IsActive",
        "CreatedAtUtc"
    ];

    public EmployeePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            Where(BuildSearchPredicate(normalized.Search));
        }

        var sortBy = ResolveSortProperty(normalized.SortBy);

        AddSort(sortBy, normalized.SortDirection);

        ApplyPaging(
            (normalized.PageNumber - 1) * normalized.PageSize,
            normalized.PageSize);
    }

    private static string ResolveSortProperty(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
            return "EmployeeCode";

        return AllowedSorts.FirstOrDefault(
                   x => string.Equals(
                       x,
                       requested,
                       StringComparison.OrdinalIgnoreCase))
               ?? "EmployeeCode";
    }

    private static Expression<Func<Employee, bool>>
        BuildSearchPredicate(string search)
    {
        search = search.Trim();

        return employee =>
            employee.EmployeeCode.Contains(search) ||
            employee.FirstName.Contains(search) ||
            employee.LastName.Contains(search) ||
            (employee.Phone != null &&
             employee.Phone.Contains(search)) ||
            (employee.JobTitle != null &&
             employee.JobTitle.Contains(search));
    }
}