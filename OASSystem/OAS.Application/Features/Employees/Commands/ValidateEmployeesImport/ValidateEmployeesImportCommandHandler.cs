using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Contracts.Features.Employees.Import;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Commands.ValidateEmployeesImport;

public sealed class ValidateEmployeesImportCommandHandler(
    IEmployeeExcelReader excelReader,
    IReadRepository<JobTitle, Guid> jobTitleRepository)
    : IRequestHandler<ValidateEmployeesImportCommand, EmployeeImportPreviewDto>
{
    public async Task<EmployeeImportPreviewDto> Handle(
        ValidateEmployeesImportCommand request,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(request.FileContent);
        IReadOnlyList<EmployeeImportRowDto> rows;
        try
        {
            rows = await excelReader.ReadAsync(stream, cancellationToken);
        }
        catch (Exception ex)
        {
            return new EmployeeImportPreviewDto(0, 0, 0, [new EmployeeImportErrorDto(1, "الملف", ex.Message)]);
        }

        var jobTitles = (await jobTitleRepository.ListAsync(cancellationToken: cancellationToken))
            .Where(x => x.IsActive)
            .Select(x => x.Name)
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        var errors = new List<EmployeeImportErrorDto>();
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.FirstName))
                errors.Add(new(row.RowNumber, "الاسم الأول", "الاسم الأول مطلوب."));
            else if (row.FirstName.Trim().Length > 100)
                errors.Add(new(row.RowNumber, "الاسم الأول", "الاسم الأول يجب ألا يتجاوز 100 حرف."));

            if (string.IsNullOrWhiteSpace(row.LastName))
                errors.Add(new(row.RowNumber, "اسم العائلة", "اسم العائلة مطلوب."));
            else if (row.LastName.Trim().Length > 100)
                errors.Add(new(row.RowNumber, "اسم العائلة", "اسم العائلة يجب ألا يتجاوز 100 حرف."));

            if (!string.IsNullOrWhiteSpace(row.Phone) && row.Phone.Trim().Length > 32)
                errors.Add(new(row.RowNumber, "رقم الهاتف", "رقم الهاتف يجب ألا يتجاوز 32 حرفًا."));

            if (!string.IsNullOrWhiteSpace(row.Email))
            {
                var email = row.Email.Trim();
                if (email.Length > 256 || !email.Contains('@') || email.StartsWith('@') || email.EndsWith('@'))
                    errors.Add(new(row.RowNumber, "البريد الإلكتروني", "البريد الإلكتروني غير صالح."));
            }

            ValidateLength(errors, row.RowNumber, "الدولة", row.Country, 100);
            ValidateLength(errors, row.RowNumber, "المحافظة", row.Governorate, 100);
            ValidateLength(errors, row.RowNumber, "المدينة", row.City, 100);
            ValidateLength(errors, row.RowNumber, "الرمز البريدي", row.PostalCode, 24);
            ValidateLength(errors, row.RowNumber, "عنوان السكن", row.ResidentialAddress, 300);

            if (string.IsNullOrWhiteSpace(row.JobTitle))
                errors.Add(new(row.RowNumber, "المسمى الوظيفي", "المسمى الوظيفي مطلوب."));
            else if (!jobTitles.Contains(row.JobTitle.Trim()))
                errors.Add(new(row.RowNumber, "المسمى الوظيفي", $"المسمى الوظيفي «{row.JobTitle.Trim()}» غير موجود أو غير نشط."));

            if (!IsBoolean(row.IsCommissionEligible))
                errors.Add(new(row.RowNumber, "مستحق للعمولة", "القيمة يجب أن تكون نعم أو لا."));
            if (!IsBoolean(row.IsActive))
                errors.Add(new(row.RowNumber, "نشط", "القيمة يجب أن تكون نعم أو لا."));
        }

        var invalidRows = errors.Select(x => x.RowNumber).Distinct().Count();
        return new EmployeeImportPreviewDto(rows.Count, rows.Count - invalidRows, invalidRows, errors);
    }

    private static void ValidateLength(List<EmployeeImportErrorDto> errors, int rowNumber, string field, string? value, int maximum)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maximum)
            errors.Add(new(rowNumber, field, $"{field} يجب ألا يتجاوز {maximum} حرفًا."));
    }

    private static bool IsBoolean(string value) =>
        value.Trim().ToLowerInvariant() is "نعم" or "لا" or "yes" or "no" or "true" or "false" or "1" or "0";
}
