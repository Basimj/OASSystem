using FluentValidation;

namespace OAS.Application.Identity.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithErrorCode("role_name_required")
            .MaximumLength(64).WithErrorCode("role_name_max_length")
            .Matches("^[A-Za-z][A-Za-z0-9_.-]*$").WithErrorCode("role_name_invalid");

        RuleFor(x => x.Request.DisplayName)
            .NotEmpty().WithErrorCode("role_display_name_required")
            .MaximumLength(100).WithErrorCode("role_display_name_max_length");
    }
}
