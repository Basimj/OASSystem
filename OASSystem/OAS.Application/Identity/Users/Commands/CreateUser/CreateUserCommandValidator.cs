using FluentValidation;

namespace OAS.Application.Identity.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private const string PhonePattern = @"^[0-9+()\-\s]{7,32}$";

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
        RuleFor(x => x.Request.PhoneNumber).NotEmpty().WithErrorCode("phone_number_required")
            .MaximumLength(32).WithErrorCode("phone_number_max_length")
            .Matches(PhonePattern).WithErrorCode("phone_number_invalid");
        RuleFor(x => x.Request.RoleIds).NotNull().WithErrorCode("roles_required").Must(x => x is { Count: 1 }).WithErrorCode("roles_single_required");
        RuleFor(x => x.Request.RoleIds).Must(x => x is null || x.Distinct().Count() == x.Count).WithErrorCode("roles_duplicate");
    }
}
