using FluentValidation;
using OAS.Application.Identity.Security;

namespace OAS.Application.Identity.Profile.Commands.ChangeMyPassword;

public sealed class ChangeMyPasswordCommandValidator : AbstractValidator<ChangeMyPasswordCommand>
{
    public ChangeMyPasswordCommandValidator()
    {
        RuleFor(x => x.Request.CurrentPassword).NotEmpty().WithErrorCode("current_password_required");
        RuleFor(x => x.Request.NewPassword).Apply();
        RuleFor(x => x.Request.ConfirmPassword).NotEmpty().Equal(x => x.Request.NewPassword).WithErrorCode("password_confirmation_mismatch");
    }
}
