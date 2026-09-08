using FluentValidation;

namespace OAS.Application.Features.Employees.Commands.SetEmployeeStatus;

public sealed class SetEmployeeStatusCommandValidator
    : AbstractValidator<SetEmployeeStatusCommand>
{
    public SetEmployeeStatusCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithErrorCode("employee_id_required");

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