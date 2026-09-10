using FluentValidation;

namespace OAS.Application.Features.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty().WithErrorCode("employee_id_required");
        RuleFor(x => x.Request.FirstName)
            .NotEmpty().WithErrorCode("first_name_required")
            .MaximumLength(100).WithErrorCode("first_name_max_length");
        RuleFor(x => x.Request.LastName)
            .NotEmpty().WithErrorCode("last_name_required")
            .MaximumLength(100).WithErrorCode("last_name_max_length");
        RuleFor(x => x.Request.Phone)
            .MaximumLength(32).WithErrorCode("phone_max_length")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Phone));
        RuleFor(x => x.Request.Email)
            .MaximumLength(256).WithErrorCode("email_max_length")
            .EmailAddress().WithErrorCode("email_invalid")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Email));
        RuleFor(x => x.Request.Country).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Request.Country));
        RuleFor(x => x.Request.Governorate).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Request.Governorate));
        RuleFor(x => x.Request.City).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Request.City));
        RuleFor(x => x.Request.PostalCode).MaximumLength(24).When(x => !string.IsNullOrWhiteSpace(x.Request.PostalCode));
        RuleFor(x => x.Request.ResidentialAddress).MaximumLength(300).When(x => !string.IsNullOrWhiteSpace(x.Request.ResidentialAddress));
        RuleFor(x => x.Request.JobTitleId)
            .NotEmpty().WithErrorCode("job_title_required");
        RuleFor(x => x.Request.RowVersion)
            .NotEmpty().WithErrorCode("row_version_required")
            .Must(IsValidBase64).WithErrorCode("row_version_invalid");
    }

    private static bool IsValidBase64(string value)
    {
        try { Convert.FromBase64String(value); return true; }
        catch (FormatException) { return false; }
    }
}
