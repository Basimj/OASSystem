using FluentValidation;

namespace OAS.Application.Identity.Users.Commands;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Request.UserName)
            .NotEmpty().WithErrorCode("user_name_required")
            .MinimumLength(3).WithErrorCode("user_name_min_length")
            .MaximumLength(64).WithErrorCode("user_name_max_length")
            .Matches(@"^[\p{L}\p{N}._-]+$").WithErrorCode("user_name_invalid_format");
        RuleFor(x => x.Request.FirstName).NotEmpty().WithErrorCode("first_name_required").MaximumLength(100).WithErrorCode("first_name_max_length");
        RuleFor(x => x.Request.LastName).NotEmpty().WithErrorCode("last_name_required").MaximumLength(100).WithErrorCode("last_name_max_length");
        RuleFor(x => x.Request.Email).EmailAddress().WithErrorCode("email_invalid").MaximumLength(256).WithErrorCode("email_max_length")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Email));
        RuleFor(x => x.Request.Password).NotEmpty().WithErrorCode("new_password_required");
        RuleFor(x => x.Request.Password).MinimumLength(8).WithErrorCode("password_min_length").When(x => !string.IsNullOrEmpty(x.Request.Password));
        RuleFor(x => x.Request.Password).MaximumLength(128).WithErrorCode("password_max_length").When(x => !string.IsNullOrEmpty(x.Request.Password));
        RuleFor(x => x.Request.Password).Matches("[A-Z]").WithErrorCode("password_uppercase_required").When(x => !string.IsNullOrEmpty(x.Request.Password));
        RuleFor(x => x.Request.Password).Matches("[a-z]").WithErrorCode("password_lowercase_required").When(x => !string.IsNullOrEmpty(x.Request.Password));
        RuleFor(x => x.Request.Password).Matches("[0-9]").WithErrorCode("password_digit_required").When(x => !string.IsNullOrEmpty(x.Request.Password));
        RuleFor(x => x.Request.Password).Matches("[^A-Za-z0-9]").WithErrorCode("password_special_required").When(x => !string.IsNullOrEmpty(x.Request.Password));
        RuleFor(x => x.Request.ConfirmPassword).NotEmpty().WithErrorCode("confirm_password_required");
        RuleFor(x => x.Request.ConfirmPassword).Equal(x => x.Request.Password).WithErrorCode("password_confirmation_mismatch")
            .When(x => !string.IsNullOrEmpty(x.Request.ConfirmPassword));
        RuleFor(x => x.Request.RoleId).NotEmpty().WithErrorCode("role_required");
    }
}
