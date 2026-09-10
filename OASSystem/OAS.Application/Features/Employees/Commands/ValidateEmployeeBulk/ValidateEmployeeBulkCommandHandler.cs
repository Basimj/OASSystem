using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Contracts.Features.Employees.Import;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Commands.ValidateEmployeeBulk;

public sealed class ValidateEmployeeBulkCommandHandler(IReadRepository<JobTitle, Guid> jobTitleRepository)
    : IRequestHandler<ValidateEmployeeBulkCommand, EmployeeBulkValidationResultDto>
{
    public async Task<EmployeeBulkValidationResultDto> Handle(
        ValidateEmployeeBulkCommand request,
        CancellationToken cancellationToken)
    {
        var rows = request.Request.Rows.Where(IsNotEmptyRow).ToArray();
        var titles = (await jobTitleRepository.ListAsync(cancellationToken: cancellationToken))
            .Where(x => x.IsActive)
            .Select(x => x.Name)
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        var errors = new List<EmployeeBulkValidationErrorDto>();
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
            else if (!titles.Contains(row.JobTitle.Trim()))
                errors.Add(new(row.RowNumber, "المسمى الوظيفي", $"المسمى الوظيفي «{row.JobTitle.Trim()}» غير موجود أو غير نشط."));
        }

        var invalid = errors.Select(x => x.RowNumber).Distinct().Count();
        return new EmployeeBulkValidationResultDto(rows.Length, rows.Length - invalid, invalid, errors);
    }

    private static void ValidateLength(List<EmployeeBulkValidationErrorDto> errors, int rowNumber, string field, string? value, int maximum)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maximum)
            errors.Add(new(rowNumber, field, $"{field} يجب ألا يتجاوز {maximum} حرفًا."));
    }

    private static bool IsNotEmptyRow(EmployeeBulkValidationRowDto row) =>
        !string.IsNullOrWhiteSpace(row.FirstName) ||
        !string.IsNullOrWhiteSpace(row.LastName) ||
        !string.IsNullOrWhiteSpace(row.Phone) ||
        !string.IsNullOrWhiteSpace(row.Email) ||
        !string.IsNullOrWhiteSpace(row.Country) ||
        !string.IsNullOrWhiteSpace(row.Governorate) ||
        !string.IsNullOrWhiteSpace(row.City) ||
        !string.IsNullOrWhiteSpace(row.PostalCode) ||
        !string.IsNullOrWhiteSpace(row.ResidentialAddress) ||
        !string.IsNullOrWhiteSpace(row.JobTitle) ||
        row.HireDate.HasValue ||
        row.IsCommissionEligible ||
        !row.IsActive;
}
