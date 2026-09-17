using FluentValidation;

namespace OAS.Application.Accounting.PostingProfiles.Commands.SetPostingProfileStatus;

public sealed class SetPostingProfileStatusCommandValidator
    : AbstractValidator<SetPostingProfileStatusCommand>
{
    public SetPostingProfileStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("posting_profile_id_required");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
