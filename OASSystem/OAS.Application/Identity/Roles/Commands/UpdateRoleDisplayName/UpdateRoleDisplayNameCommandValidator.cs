using FluentValidation;

namespace OAS.Application.Identity.Roles.Commands.UpdateRoleDisplayName;

public sealed class UpdateRoleDisplayNameCommandValidator : AbstractValidator<UpdateRoleDisplayNameCommand>
{
    public UpdateRoleDisplayNameCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty().WithErrorCode("role_id_required");
        RuleFor(x => x.Request.DisplayName)
            .NotEmpty().WithErrorCode("role_display_name_required")
            .MaximumLength(100).WithErrorCode("role_display_name_max_length");
    }
}
