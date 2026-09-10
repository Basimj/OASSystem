using OAS.Contracts.Features.Employees;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Features.Employees.Mapping;

public static class EmployeeMapping
{
    public static EmployeeDto ToDto(Employee employee, JobTitle jobTitle, UserAccount? user = null) => new(
        employee.Id,
        employee.EmployeeNumber,
        employee.EmployeeCode,
        employee.FirstName,
        employee.LastName,
        employee.DisplayName,
        employee.ContactInfo.Phone,
        employee.ContactInfo.Email,
        employee.ContactInfo.Address.Country,
        employee.ContactInfo.Address.Governorate,
        employee.ContactInfo.Address.City,
        employee.ContactInfo.Address.PostalCode,
        employee.ContactInfo.Address.ResidentialAddress,
        employee.JobTitleId,
        jobTitle.Name,
        employee.HireDate,
        employee.IsCommissionEligible,
        employee.IsActive,
        employee.UserAccountId,
        user?.DisplayName,
        user?.UserName,
        user?.Email,
        employee.Photo,
        Convert.ToBase64String(employee.RowVersion),
        employee.CreatedAtUtc,
        employee.LastModifiedAtUtc);
}
