using FluentValidation;
using OAS.Application.Identity.Users.Common;

namespace OAS.Application.Identity.Users.Commands.SetUserStatus;

public sealed class SetUserStatusCommandValidator : AbstractValidator<SetUserStatusCommand>
{
    public SetUserStatusCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithErrorCode("user_id_required");
        RuleFor(x => x.Request.RowVersion).Must(RowVersionCodec.IsValid).WithErrorCode("row_version_invalid");
    }
}
