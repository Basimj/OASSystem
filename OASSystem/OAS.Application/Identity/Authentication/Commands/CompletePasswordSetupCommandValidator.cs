using FluentValidation;
using OAS.Application.Identity.Security;

namespace OAS.Application.Identity.Authentication.Commands;

public sealed class CompletePasswordSetupCommandValidator : AbstractValidator<CompletePasswordSetupCommand>
{
    public CompletePasswordSetupCommandValidator()
    {
        RuleFor(x => x.Request.NewPassword).Apply();
        RuleFor(x => x.Request.ConfirmPassword)
            .NotEmpty().WithErrorCode("confirm_password_required")
            .Equal(x => x.Request.NewPassword).WithErrorCode("password_confirmation_mismatch");
    }
}
