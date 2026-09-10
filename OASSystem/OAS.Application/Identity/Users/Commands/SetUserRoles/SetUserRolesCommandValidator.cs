using FluentValidation;
using OAS.Application.Identity.Users.Common;

namespace OAS.Application.Identity.Users.Commands.SetUserRoles;

public sealed class SetUserRolesCommandValidator : AbstractValidator<SetUserRolesCommand>
{
    public SetUserRolesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithErrorCode("user_id_required");
        RuleFor(x => x.Request.RoleIds).NotNull().WithErrorCode("roles_required").Must(x => x is { Count: 1 }).WithErrorCode("roles_single_required");
        RuleFor(x => x.Request.RoleIds).Must(x => x is null || x.Distinct().Count() == x.Count).WithErrorCode("roles_duplicate");
        RuleFor(x => x.Request.RowVersion).Must(RowVersionCodec.IsValid).WithErrorCode("row_version_invalid");
    }
}
