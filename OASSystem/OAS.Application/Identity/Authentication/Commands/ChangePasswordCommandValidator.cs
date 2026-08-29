using FluentValidation;

namespace OAS.Application.Identity.Authentication.Commands;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.Request.CurrentPassword)
            .NotEmpty().WithErrorCode("current_password_required");

        RuleFor(x => x.Request.NewPassword)
            .NotEmpty().WithErrorCode("new_password_required");

        RuleFor(x => x.Request.NewPassword)
            .MinimumLength(8).WithErrorCode("password_min_length")
            .When(x => !string.IsNullOrEmpty(x.Request.NewPassword));

        RuleFor(x => x.Request.NewPassword)
            .MaximumLength(128).WithErrorCode("password_max_length")
            .When(x => !string.IsNullOrEmpty(x.Request.NewPassword));

        RuleFor(x => x.Request.NewPassword)
            .Matches("[A-Z]").WithErrorCode("password_uppercase_required")
            .When(x => !string.IsNullOrEmpty(x.Request.NewPassword));

        RuleFor(x => x.Request.NewPassword)
            .Matches("[a-z]").WithErrorCode("password_lowercase_required")
            .When(x => !string.IsNullOrEmpty(x.Request.NewPassword));

        RuleFor(x => x.Request.NewPassword)
            .Matches("[0-9]").WithErrorCode("password_digit_required")
            .When(x => !string.IsNullOrEmpty(x.Request.NewPassword));

        RuleFor(x => x.Request.NewPassword)
            .Matches("[^A-Za-z0-9]").WithErrorCode("password_special_required")
            .When(x => !string.IsNullOrEmpty(x.Request.NewPassword));

        RuleFor(x => x.Request.ConfirmPassword)
            .NotEmpty().WithErrorCode("confirm_password_required");

        RuleFor(x => x.Request.ConfirmPassword)
            .Equal(x => x.Request.NewPassword).WithErrorCode("password_confirmation_mismatch")
            .When(x => !string.IsNullOrEmpty(x.Request.ConfirmPassword));
    }
}
