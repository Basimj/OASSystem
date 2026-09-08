using FluentValidation;

namespace OAS.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandValidator
    : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
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
    }
}