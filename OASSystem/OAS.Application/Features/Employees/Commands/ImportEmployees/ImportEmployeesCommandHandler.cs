using OAS.Application.Abstractions.Persistence;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Application.Features.Employees.Specifications;
using OAS.Contracts.Features.Employees.Import;
using OAS.Domain.Features.Employees.Entities;
using MediatR;

namespace OAS.Application.Features.Employees.Commands.ImportEmployees;

public sealed class ImportEmployeesCommandHandler(
    IEmployeeExcelReader excelReader,
    IRepository<Employee, Guid> employeeRepository,
    IReadRepository<Employee, Guid> readEmployeeRepository)
    : IRequestHandler<
        ImportEmployeesCommand,
        EmployeeImportResultDto>
{
    public async Task<EmployeeImportResultDto> Handle(
        ImportEmployeesCommand request,
        CancellationToken cancellationToken)
    {
        using var stream =
            new MemoryStream(request.FileContent);

        var rows =
            await excelReader.ReadAsync(
                stream,
                cancellationToken);

        var seenCodes =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var validRows =
            new List<EmployeeImportRowDto>();

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedCode =
                row.EmployeeCode
                    .Trim()
                    .ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(normalizedCode))
                continue;

            if (!seenCodes.Add(normalizedCode))
                continue;

            var exists =
                await readEmployeeRepository.CountAsync(
                    new EmployeeCodeSpecification(
                        normalizedCode),
                    cancellationToken);

            if (exists > 0)
                continue;

            var employee =
                Employee.Create(
                    Guid.NewGuid(),
                    row.EmployeeCode,
                    row.FirstName,
                    row.LastName,
                    NullIfEmpty(row.Phone),
                    NullIfEmpty(row.JobTitle),
                    row.HireDate,
                    NullIfEmpty(row.Notes),
                    ParseBoolean(row.IsSalesperson),
                    ParseBoolean(row.IsTechnician),
                    ParseBoolean(row.IsCommissionEligible),
                    ParseBoolean(row.IsActive),
                    null);

            await employeeRepository.AddAsync(
                employee,
                cancellationToken);

            validRows.Add(row);
        }

        return new EmployeeImportResultDto(
            validRows.Count);
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static bool ParseBoolean(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "نعم" => true,
            "yes" => true,
            "true" => true,
            "1" => true,

            _ => false
        };
    }
}