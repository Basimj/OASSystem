using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Application.Features.Employees.Specifications;
using OAS.Contracts.Features.Employees.Import;
using OAS.Domain.Features.Employees.Entities;
using MediatR;

namespace OAS.Application.Features.Employees.Commands.ValidateEmployeesImport;

public sealed class ValidateEmployeesImportCommandHandler(
    IEmployeeExcelReader excelReader,
    IReadRepository<Employee, Guid> employeeRepository)
    : IRequestHandler<
        ValidateEmployeesImportCommand,
        EmployeeImportPreviewDto>
{
    public async Task<EmployeeImportPreviewDto> Handle(
        ValidateEmployeesImportCommand request,
        CancellationToken cancellationToken)
    {
        using var stream =
            new MemoryStream(request.FileContent);

        IReadOnlyList<EmployeeImportRowDto> rows;

        try
        {
            rows = await excelReader.ReadAsync(
                stream,
                cancellationToken);
        }
        catch (Exception ex)
        {
            return new EmployeeImportPreviewDto(
                0,
                0,
                0,
                [
                    new EmployeeImportErrorDto(
                        1,
                        "الملف",
                        ex.Message)
                ]);
        }

        var errors =
            new List<EmployeeImportErrorDto>();

        var seenCodes =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            ValidateBasicFields(
                row,
                errors);

            ValidateBooleanFields(
                row,
                errors);

            ValidateDate(
                row,
                errors);

            var normalizedCode =
                row.EmployeeCode
                    .Trim()
                    .ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(normalizedCode))
            {
                if (seenCodes.TryGetValue(
                        normalizedCode,
                        out var firstRow))
                {
                    errors.Add(
                        new EmployeeImportErrorDto(
                            row.RowNumber,
                            "رمز الموظف",
                            $"رمز الموظف مكرر داخل الملف. ظهر أولاً في الصف {firstRow}."));
                }
                else
                {
                    seenCodes[normalizedCode] =
                        row.RowNumber;
                }

                try
                {
                    var exists =
                        await employeeRepository.CountAsync(
                            new EmployeeCodeSpecification(
                                normalizedCode),
                            cancellationToken);

                    if (exists > 0)
                    {
                        errors.Add(
                            new EmployeeImportErrorDto(
                                row.RowNumber,
                                "رمز الموظف",
                                "رمز الموظف موجود مسبقًا في قاعدة البيانات."));
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(
                        new EmployeeImportErrorDto(
                            row.RowNumber,
                            "رمز الموظف",
                            $"تعذر التحقق من قاعدة البيانات: {ex.Message}"));
                }
            }
        }

        var invalidRows =
            rows
                .Select(x => x.RowNumber)
                .Distinct()
                .Count(rowNumber =>
                    errors.Any(e =>
                        e.RowNumber == rowNumber));

        return new EmployeeImportPreviewDto(
            rows.Count,
            rows.Count - invalidRows,
            invalidRows,
            errors);
    }

    private static void ValidateBasicFields(
        EmployeeImportRowDto row,
        List<EmployeeImportErrorDto> errors)
    {
        if (string.IsNullOrWhiteSpace(row.EmployeeCode))
        {
            errors.Add(
                new EmployeeImportErrorDto(
                    row.RowNumber,
                    "رمز الموظف",
                    "رمز الموظف مطلوب."));
        }
        else if (row.EmployeeCode.Trim().Length > 32)
        {
            errors.Add(
                new EmployeeImportErrorDto(
                    row.RowNumber,
                    "رمز الموظف",
                    "رمز الموظف يجب ألا يتجاوز 32 حرفًا."));
        }

        if (string.IsNullOrWhiteSpace(row.FirstName))
        {
            errors.Add(
                new EmployeeImportErrorDto(
                    row.RowNumber,
                    "الاسم الأول",
                    "الاسم الأول مطلوب."));
        }
        else if (row.FirstName.Trim().Length > 100)
        {
            errors.Add(
                new EmployeeImportErrorDto(
                    row.RowNumber,
                    "الاسم الأول",
                    "الاسم الأول يجب ألا يتجاوز 100 حرف."));
        }

        if (string.IsNullOrWhiteSpace(row.LastName))
        {
            errors.Add(
                new EmployeeImportErrorDto(
                    row.RowNumber,
                    "اسم العائلة",
                    "اسم العائلة مطلوب."));
        }
        else if (row.LastName.Trim().Length > 100)
        {
            errors.Add(
                new EmployeeImportErrorDto(
                    row.RowNumber,
                    "اسم العائلة",
                    "اسم العائلة يجب ألا يتجاوز 100 حرف."));
        }

        if (row.Phone?.Length > 32)
        {
            errors.Add(
                new EmployeeImportErrorDto(
                    row.RowNumber,
                    "رقم الهاتف",
                    "رقم الهاتف يجب ألا يتجاوز 32 حرفًا."));
        }

        if (row.JobTitle?.Length > 100)
        {
            errors.Add(
                new EmployeeImportErrorDto(
                    row.RowNumber,
                    "المسمى الوظيفي",
                    "المسمى الوظيفي يجب ألا يتجاوز 100 حرف."));
        }

        if (row.Notes?.Length > 1000)
        {
            errors.Add(
                new EmployeeImportErrorDto(
                    row.RowNumber,
                    "الملاحظات",
                    "الملاحظات يجب ألا تتجاوز 1000 حرف."));
        }
    }

    private static void ValidateBooleanFields(
       EmployeeImportRowDto row,
       List<EmployeeImportErrorDto> errors)
    {
        ValidateBoolean(
            row,
            row.IsSalesperson,
            "موظف مبيعات",
            errors);

        ValidateBoolean(
            row,
            row.IsTechnician,
            "فني",
            errors);

        ValidateBoolean(
            row,
            row.IsCommissionEligible,
            "مستحق للعمولة",
            errors);

        ValidateBoolean(
            row,
            row.IsActive,
            "نشط",
            errors);
    }

    private static void ValidateBoolean(
        EmployeeImportRowDto row,
        string value,
        string column,
        List<EmployeeImportErrorDto> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(
                new EmployeeImportErrorDto(
                    row.RowNumber,
                    column,
                    $"الحقل «{column}» مطلوب ويجب أن يكون نعم أو لا."));

            return;
        }

        var normalized =
            value.Trim().ToLowerInvariant();

        var valid =
            normalized is
                "نعم" or
                "لا" or
                "yes" or
                "no" or
                "true" or
                "false" or
                "1" or
                "0";

        if (!valid)
        {
            errors.Add(
                new EmployeeImportErrorDto(
                    row.RowNumber,
                    column,
                    "القيمة يجب أن تكون نعم أو لا."));
        }
    }

    private static void ValidateDate(
       EmployeeImportRowDto row,
       List<EmployeeImportErrorDto> errors)
    {
        if (string.IsNullOrWhiteSpace(row.HireDate?.ToString()))
        {
            return;
        }
    }
}