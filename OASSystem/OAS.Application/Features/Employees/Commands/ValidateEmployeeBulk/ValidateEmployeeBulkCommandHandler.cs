using System.Text.RegularExpressions;
using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Features.Employees.Specifications;
using OAS.Contracts.Features.Employees.Import;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Commands.ValidateEmployeeBulk;

public sealed class ValidateEmployeeBulkCommandHandler(
    IReadRepository<Employee, Guid> employeeRepository)
    : IRequestHandler<
        ValidateEmployeeBulkCommand,
        EmployeeBulkValidationResultDto>
{
    private static readonly Regex EmployeeCodeRegex =
        new(
            "^[A-Za-z0-9_-]+$",
            RegexOptions.Compiled |
            RegexOptions.CultureInvariant);

    public async Task<EmployeeBulkValidationResultDto> Handle(
        ValidateEmployeeBulkCommand request,
        CancellationToken cancellationToken)
    {
        var rows =
            request.Request.Rows
                .Where(IsNotEmptyRow)
                .ToList();

        var errors =
            new List<EmployeeBulkValidationErrorDto>();

        if (rows.Count == 0)
        {
            return new EmployeeBulkValidationResultDto(
                0,
                0,
                0,
                errors);
        }

        /*
         * ============================================
         * 1. التحقق الأساسي لكل صف
         * ============================================
         */

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateBasicFields(
                row,
                errors);

            ValidateEmployeeCode(
                row,
                errors);

            ValidatePhone(
                row,
                errors);
        }


        /*
         * ============================================
         * 2. التحقق من التكرار داخل البيانات المدخلة
         * ============================================
         */

        ValidateDuplicateCodesInsideRequest(
            rows,
            errors);


        /*
         * ============================================
         * 3. التحقق من قاعدة البيانات
         * ============================================
         */

        await ValidateCodesAgainstDatabaseAsync(
            rows,
            errors,
            cancellationToken);


        /*
         * ============================================
         * 4. حساب النتيجة
         * ============================================
         */

        var invalidRows =
            errors
                .Select(error => error.RowNumber)
                .Distinct()
                .ToHashSet();

        var validRows =
            rows.Count(
                row =>
                    !invalidRows.Contains(
                        row.RowNumber));

        return new EmployeeBulkValidationResultDto(
            rows.Count,
            validRows,
            rows.Count - validRows,
            errors);
    }


    private static bool IsNotEmptyRow(
        EmployeeBulkValidationRowDto row)
    {
        return
            !string.IsNullOrWhiteSpace(
                row.EmployeeCode) ||
            !string.IsNullOrWhiteSpace(
                row.FirstName) ||
            !string.IsNullOrWhiteSpace(
                row.LastName) ||
            !string.IsNullOrWhiteSpace(
                row.Phone) ||
            !string.IsNullOrWhiteSpace(
                row.JobTitle) ||
            row.HireDate.HasValue ||
            row.IsSalesperson ||
            row.IsTechnician ||
            row.IsCommissionEligible ||
            !row.IsActive;
    }


    /*
     * ============================================
     * Basic Fields
     * ============================================
     */

    private static void ValidateBasicFields(
        EmployeeBulkValidationRowDto row,
        List<EmployeeBulkValidationErrorDto> errors)
    {
        if (string.IsNullOrWhiteSpace(
                row.EmployeeCode))
        {
            errors.Add(
                new EmployeeBulkValidationErrorDto(
                    row.RowNumber,
                    "رمز الموظف",
                    "رمز الموظف مطلوب."));
        }

        if (string.IsNullOrWhiteSpace(
                row.FirstName))
        {
            errors.Add(
                new EmployeeBulkValidationErrorDto(
                    row.RowNumber,
                    "الاسم الأول",
                    "الاسم الأول مطلوب."));
        }
        else if (row.FirstName.Trim().Length > 100)
        {
            errors.Add(
                new EmployeeBulkValidationErrorDto(
                    row.RowNumber,
                    "الاسم الأول",
                    "الاسم الأول يجب ألا يتجاوز 100 حرف."));
        }

        if (string.IsNullOrWhiteSpace(
                row.LastName))
        {
            errors.Add(
                new EmployeeBulkValidationErrorDto(
                    row.RowNumber,
                    "اسم العائلة",
                    "اسم العائلة مطلوب."));
        }
        else if (row.LastName.Trim().Length > 100)
        {
            errors.Add(
                new EmployeeBulkValidationErrorDto(
                    row.RowNumber,
                    "اسم العائلة",
                    "اسم العائلة يجب ألا يتجاوز 100 حرف."));
        }

        if (row.Phone is not null &&
            row.Phone.Length > 32)
        {
            errors.Add(
                new EmployeeBulkValidationErrorDto(
                    row.RowNumber,
                    "رقم الهاتف",
                    "رقم الهاتف يجب ألا يتجاوز 32 رقمًا."));
        }

        if (row.JobTitle is not null &&
            row.JobTitle.Length > 100)
        {
            errors.Add(
                new EmployeeBulkValidationErrorDto(
                    row.RowNumber,
                    "المسمى الوظيفي",
                    "المسمى الوظيفي يجب ألا يتجاوز 100 حرف."));
        }
    }


    /*
     * ============================================
     * Employee Code
     * ============================================
     */

    private static void ValidateEmployeeCode(
        EmployeeBulkValidationRowDto row,
        List<EmployeeBulkValidationErrorDto> errors)
    {
        if (string.IsNullOrWhiteSpace(
                row.EmployeeCode))
        {
            return;
        }

        var code =
            row.EmployeeCode.Trim();

        if (code.Length > 32)
        {
            errors.Add(
                new EmployeeBulkValidationErrorDto(
                    row.RowNumber,
                    "رمز الموظف",
                    "رمز الموظف يجب ألا يتجاوز 32 حرفًا."));

            return;
        }

        if (!EmployeeCodeRegex.IsMatch(code))
        {
            errors.Add(
                new EmployeeBulkValidationErrorDto(
                    row.RowNumber,
                    "رمز الموظف",
                    "رمز الموظف يجب أن يحتوي على أحرف إنجليزية وأرقام فقط، مع السماح بـ - و _."));
        }
    }


    /*
     * ============================================
     * Phone
     * ============================================
     */

    private static void ValidatePhone(
        EmployeeBulkValidationRowDto row,
        List<EmployeeBulkValidationErrorDto> errors)
    {
        if (string.IsNullOrWhiteSpace(row.Phone))
        {
            return;
        }

        if (row.Phone.Any(
                character =>
                    !char.IsDigit(character)))
        {
            errors.Add(
                new EmployeeBulkValidationErrorDto(
                    row.RowNumber,
                    "رقم الهاتف",
                    "رقم الهاتف يجب أن يحتوي على أرقام فقط."));
        }
    }


    /*
     * ============================================
     * Duplicate Codes Inside Request
     * ============================================
     */

    private static void ValidateDuplicateCodesInsideRequest(
        IReadOnlyCollection<EmployeeBulkValidationRowDto> rows,
        List<EmployeeBulkValidationErrorDto> errors)
    {
        var groups =
            rows
                .Where(
                    row =>
                        !string.IsNullOrWhiteSpace(
                            row.EmployeeCode))
                .GroupBy(
                    row =>
                        row.EmployeeCode.Trim(),
                    StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var groupedRows =
                group.ToList();

            if (groupedRows.Count <= 1)
            {
                continue;
            }

            foreach (var row in groupedRows)
            {
                errors.Add(
                    new EmployeeBulkValidationErrorDto(
                        row.RowNumber,
                        "رمز الموظف",
                        $"رمز الموظف «{row.EmployeeCode.Trim()}» مكرر داخل البيانات المدخلة."));
            }
        }
    }


    /*
     * ============================================
     * Database Validation
     * ============================================
     */

    private async Task ValidateCodesAgainstDatabaseAsync(
        IReadOnlyCollection<EmployeeBulkValidationRowDto> rows,
        List<EmployeeBulkValidationErrorDto> errors,
        CancellationToken cancellationToken)
    {
        var codes =
            rows
                .Where(
                    row =>
                        !string.IsNullOrWhiteSpace(
                            row.EmployeeCode))
                .Select(
                    row =>
                        new
                        {
                            row.RowNumber,
                            Code =
                                row.EmployeeCode.Trim()
                        })
                .DistinctBy(
                    x =>
                        x.Code,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        foreach (var item in codes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var exists =
                await employeeRepository.CountAsync(
                    new EmployeeCodeSpecification(
                        item.Code),
                    cancellationToken);

            if (exists <= 0)
            {
                continue;
            }

            foreach (var row in rows.Where(
                row =>
                    !string.IsNullOrWhiteSpace(
                        row.EmployeeCode) &&
                    string.Equals(
                        row.EmployeeCode.Trim(),
                        item.Code,
                        StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add(
                    new EmployeeBulkValidationErrorDto(
                        row.RowNumber,
                        "رمز الموظف",
                        $"رمز الموظف «{item.Code}» موجود مسبقًا في قاعدة البيانات."));
            }
        }
    }
}