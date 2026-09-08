using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Contracts.Features.Employees;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Queries.ExportEmployees;

public sealed class ExportEmployeesQueryHandler(
    IReadRepository<Employee, Guid> employeeRepository,
    IEmployeeExcelExporter excelExporter)
    : IRequestHandler<
        ExportEmployeesQuery,
        byte[]>
{
    public async Task<byte[]> Handle(
        ExportEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var employees =
            await employeeRepository.ListAsync(
                cancellationToken: cancellationToken);

        var rows =
            employees
                .Select(employee =>
                    new EmployeeExportRow(
                        employee.EmployeeCode,
                        employee.FirstName,
                        employee.LastName,
                        employee.Phone,
                        employee.JobTitle,
                        employee.HireDate
                            ?.ToDateTime(TimeOnly.MinValue),
                        employee.IsSalesperson,
                        employee.IsTechnician,
                        employee.IsCommissionEligible,
                        employee.IsActive,
                        employee.Notes))
                .ToArray();

        return excelExporter.Export(rows);
    }
}