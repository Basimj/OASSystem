using FluentValidation;

namespace OAS.Application.Identity.Authentication.Commands;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request.Login)
            .NotEmpty().WithErrorCode("login_required")
            .MaximumLength(256).WithErrorCode("login_max_length");
        RuleFor(x => x.Request.Password)
            .NotEmpty().WithErrorCode("password_required")
            .MaximumLength(256).WithErrorCode("login_password_max_length");
    }
}
