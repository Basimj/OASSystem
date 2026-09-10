using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Queries.ExportEmployees;

public sealed class ExportEmployeesQueryHandler(
    IReadRepository<Employee, Guid> employeeRepository,
    IReadRepository<JobTitle, Guid> jobTitleRepository,
    IEmployeeExcelExporter excelExporter)
    : IRequestHandler<ExportEmployeesQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportEmployeesQuery request, CancellationToken cancellationToken)
    {
        var employees = await employeeRepository.ListAsync(cancellationToken: cancellationToken);
        var titles = (await jobTitleRepository.ListAsync(cancellationToken: cancellationToken))
            .ToDictionary(x => x.Id, x => x.Name);

        var rows = employees.Select(employee => new EmployeeExportRow(
            employee.EmployeeCode,
            employee.FirstName,
            employee.LastName,
            employee.ContactInfo.Phone,
            employee.ContactInfo.Email,
            employee.ContactInfo.Address.Country,
            employee.ContactInfo.Address.Governorate,
            employee.ContactInfo.Address.City,
            employee.ContactInfo.Address.PostalCode,
            employee.ContactInfo.Address.ResidentialAddress,
            titles.TryGetValue(employee.JobTitleId, out var title) ? title : "غير محدد",
            employee.HireDate?.ToDateTime(TimeOnly.MinValue),
            employee.IsCommissionEligible,
            employee.IsActive)).ToArray();

        return excelExporter.Export(rows);
    }
}
