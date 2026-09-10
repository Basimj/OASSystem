using FluentValidation;

namespace OAS.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
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
        RuleFor(x => x.Request.EmployeeNumber)
            .GreaterThan(0).WithErrorCode("employee_number_invalid")
            .When(x => x.Request.EmployeeNumber.HasValue);
    }
}
