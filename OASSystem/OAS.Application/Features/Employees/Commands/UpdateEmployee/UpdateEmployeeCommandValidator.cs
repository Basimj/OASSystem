using FluentValidation;

namespace OAS.Application.Features.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandValidator
    : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithErrorCode("employee_id_required");

        RuleFor(x => x.Request.EmployeeCode)
            .NotEmpty()
            .WithErrorCode("employee_code_required")
            .MaximumLength(32)
            .WithErrorCode("employee_code_max_length");

        RuleFor(x => x.Request.FirstName)
            .NotEmpty()
            .WithErrorCode("first_name_required")
            .MaximumLength(100)
            .WithErrorCode("first_name_max_length");

        RuleFor(x => x.Request.LastName)
            .NotEmpty()
            .WithErrorCode("last_name_required")
            .MaximumLength(100)
            .WithErrorCode("last_name_max_length");

        RuleFor(x => x.Request.Phone)
            .MaximumLength(32)
            .WithErrorCode("phone_max_length")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Phone));

        RuleFor(x => x.Request.JobTitle)
            .MaximumLength(100)
            .WithErrorCode("job_title_max_length")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.JobTitle));

        RuleFor(x => x.Request.Notes)
            .MaximumLength(1000)
            .WithErrorCode("notes_max_length")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Notes));

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required")
            .Must(IsValidBase64)
            .WithErrorCode("row_version_invalid");
    }

    private static bool IsValidBase64(string value)
    {
        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}