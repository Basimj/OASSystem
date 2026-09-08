using FluentValidation;
using OAS.Application.Identity.Users.Common;

namespace OAS.Application.Identity.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithErrorCode("user_id_required");
        RuleFor(x => x.Request.UserName).NotEmpty().WithErrorCode("user_name_required").MinimumLength(3).WithErrorCode("user_name_min_length").MaximumLength(64).WithErrorCode("user_name_max_length").Matches(@"^[\p{L}\p{N}._-]+$").WithErrorCode("user_name_invalid_format");
        RuleFor(x => x.Request.FirstName).NotEmpty().WithErrorCode("first_name_required").MaximumLength(100).WithErrorCode("first_name_max_length");
        RuleFor(x => x.Request.LastName).NotEmpty().WithErrorCode("last_name_required").MaximumLength(100).WithErrorCode("last_name_max_length");
        RuleFor(x => x.Request.Email).EmailAddress().WithErrorCode("email_invalid").MaximumLength(256).WithErrorCode("email_max_length").When(x => !string.IsNullOrWhiteSpace(x.Request.Email));
        RuleFor(x => x.Request.RowVersion).Must(RowVersionCodec.IsValid).WithErrorCode("row_version_invalid");
    }
}
