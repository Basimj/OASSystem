using FluentValidation;

namespace OAS.Application.Identity.Security;

public static class IdentityPasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 128;

    public static IRuleBuilderOptions<T, string> Apply<T>(this IRuleBuilderInitial<T, string> rule)
    {
        return rule
            .NotEmpty().WithErrorCode("new_password_required")
            .MinimumLength(MinimumLength).WithErrorCode("password_min_length")
            .MaximumLength(MaximumLength).WithErrorCode("password_max_length")
            .Matches("[A-Z]").WithErrorCode("password_uppercase_required")
            .Matches("[a-z]").WithErrorCode("password_lowercase_required")
            .Matches("[0-9]").WithErrorCode("password_digit_required")
            .Matches("[^A-Za-z0-9]").WithErrorCode("password_special_required");
    }
}
