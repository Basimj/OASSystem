using FluentValidation;

namespace OAS.Application.Features.Employees.JobTitles.Commands.UpdateJobTitle;

public sealed class UpdateJobTitleCommandValidator : AbstractValidator<UpdateJobTitleCommand>
{
    public UpdateJobTitleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithErrorCode("job_title_name_required")
            .MaximumLength(100).WithErrorCode("job_title_name_max_length");
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
