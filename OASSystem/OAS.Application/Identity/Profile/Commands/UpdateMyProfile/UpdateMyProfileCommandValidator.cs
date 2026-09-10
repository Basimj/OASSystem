using FluentValidation;

namespace OAS.Application.Identity.Profile.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    private const string PhonePattern = @"^[0-9+()\-\s]{7,32}$";

    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.PhoneNumber).NotEmpty().WithErrorCode("phone_number_required")
            .MaximumLength(32).WithErrorCode("phone_number_max_length")
            .Matches(PhonePattern).WithErrorCode("phone_number_invalid");
        RuleFor(x => x.Request.RowVersion).NotEmpty();
    }
}
