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
        "IsActive",
        "CreatedAtUtc"
    ];

    public EmployeePageSpecification(
        PageRequest request,
        IReadOnlyCollection<Guid>? matchingJobTitleIds = null)
    {
        var normalized = request.Normalize();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            Where(BuildSearchPredicate(
                normalized.Search,
                matchingJobTitleIds ?? []));
        }

        AddSort(
            ResolveSortProperty(normalized.SortBy),
            normalized.SortDirection);

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

    private static Expression<Func<Employee, bool>> BuildSearchPredicate(
        string search,
        IReadOnlyCollection<Guid> matchingJobTitleIds)
    {
        search = search.Trim();

        return employee =>
            employee.EmployeeCode.Contains(search) ||
            employee.FirstName.Contains(search) ||
            employee.LastName.Contains(search) ||
            (employee.ContactInfo.Phone != null &&
             employee.ContactInfo.Phone.Contains(search)) ||
            (employee.ContactInfo.Email != null &&
             employee.ContactInfo.Email.Contains(search)) ||
            (employee.ContactInfo.Address.Country != null &&
             employee.ContactInfo.Address.Country.Contains(search)) ||
            (employee.ContactInfo.Address.Governorate != null &&
             employee.ContactInfo.Address.Governorate.Contains(search)) ||
            (employee.ContactInfo.Address.City != null &&
             employee.ContactInfo.Address.City.Contains(search)) ||
            (employee.ContactInfo.Address.PostalCode != null &&
             employee.ContactInfo.Address.PostalCode.Contains(search)) ||
            (employee.ContactInfo.Address.ResidentialAddress != null &&
             employee.ContactInfo.Address.ResidentialAddress.Contains(search)) ||
            matchingJobTitleIds.Contains(employee.JobTitleId);
    }
}