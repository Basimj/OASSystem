using FluentValidation;

namespace OAS.Application.Features.Employees.JobTitles.Commands.CreateJobTitle;

public sealed class CreateJobTitleCommandValidator : AbstractValidator<CreateJobTitleCommand>
{
    public CreateJobTitleCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithErrorCode("job_title_name_required")
            .MaximumLength(100).WithErrorCode("job_title_name_max_length");
    }
}
