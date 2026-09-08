using OAS.Contracts.Features.Employees;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Mapping;

public static class EmployeeMapping
{
    public static EmployeeDto ToDto(Employee employee)
    {
        return new EmployeeDto(
            employee.Id,
            employee.EmployeeCode,
            employee.FirstName,
            employee.LastName,
            employee.DisplayName,
            employee.Phone,
            employee.JobTitle,
            employee.HireDate,
            employee.Notes,
            employee.IsSalesperson,
            employee.IsTechnician,
            employee.IsCommissionEligible,
            employee.IsActive,
            employee.UserAccountId,
            Convert.ToBase64String(employee.RowVersion),
            employee.CreatedAtUtc,
            employee.LastModifiedAtUtc);
    }
}