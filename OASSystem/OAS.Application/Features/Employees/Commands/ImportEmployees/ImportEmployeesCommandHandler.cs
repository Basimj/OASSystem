using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Contracts.Features.Employees.Import;
using OAS.Domain.Features.Employees;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.ValueObjects;

namespace OAS.Application.Features.Employees.Commands.ImportEmployees;

public sealed class ImportEmployeesCommandHandler(
    IEmployeeExcelReader excelReader,
    IRepository<Employee, Guid> employeeRepository,
    IReadRepository<JobTitle, Guid> jobTitleRepository,
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<ImportEmployeesCommand, EmployeeImportResultDto>
{
    public async Task<EmployeeImportResultDto> Handle(ImportEmployeesCommand request, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(request.FileContent);
        var rows = await excelReader.ReadAsync(stream, cancellationToken);
        var jobTitles = (await jobTitleRepository.ListAsync(cancellationToken: cancellationToken))
            .Where(x => x.IsActive)
            .ToDictionary(x => x.Name, x => x, StringComparer.CurrentCultureIgnoreCase);

        var imported = 0;
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(row.FirstName) || string.IsNullOrWhiteSpace(row.LastName) ||
                string.IsNullOrWhiteSpace(row.JobTitle) || !jobTitles.TryGetValue(row.JobTitle.Trim(), out var jobTitle))
                continue;

            var next = await sequenceNumberGenerator.NextAsync(
                "EmployeeNumberSequence",
                cancellationToken);

            if (next <= 0)
                throw new InvalidOperationException(
                    "Employee code sequence exceeded the supported range.");

            var employeeCode = EmployeeCodeFormatter.Format(next);
            var contactInfo = ContactInfo.Create(
                NullIfEmpty(row.Phone),
                NullIfEmpty(row.Email),
                Address.Create(
                    NullIfEmpty(row.Country),
                    NullIfEmpty(row.Governorate),
                    NullIfEmpty(row.City),
                    NullIfEmpty(row.PostalCode),
                    NullIfEmpty(row.ResidentialAddress)));

            var employee = Employee.Create(
                Guid.NewGuid(),
                employeeCode,
                row.FirstName,
                row.LastName,
                contactInfo,
                jobTitle.Id,
                row.HireDate,
                ParseBoolean(row.IsCommissionEligible),
                ParseBoolean(row.IsActive),
                null);

            await employeeRepository.AddAsync(employee, cancellationToken);
            imported++;
        }

        return new EmployeeImportResultDto(imported);
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool ParseBoolean(string value) =>
        value.Trim().ToLowerInvariant() is "نعم" or "yes" or "true" or "1";
}
