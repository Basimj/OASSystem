using FluentValidation;

namespace OAS.Application.Accounting.PostingProfiles.Commands.CreatePostingProfile;

public sealed class CreatePostingProfileCommandValidator
    : AbstractValidator<CreatePostingProfileCommand>
{
    public CreatePostingProfileCommandValidator()
    {
        RuleFor(x => x.Data.Code)
            .NotEmpty()
            .WithErrorCode("posting_profile_code_required")
            .MaximumLength(40)
            .WithErrorCode("posting_profile_code_max_length");

        RuleFor(x => x.Data.Name)
            .NotEmpty()
            .WithErrorCode("posting_profile_name_required")
            .MaximumLength(150)
            .WithErrorCode("posting_profile_name_max_length");

        RuleFor(x => x.Data.Module)
            .NotEmpty()
            .WithErrorCode("posting_profile_module_required")
            .MaximumLength(50)
            .WithErrorCode("posting_profile_module_max_length");

        RuleFor(x => x.Data.DocumentType)
            .NotEmpty()
            .WithErrorCode("posting_profile_document_type_required")
            .MaximumLength(50)
            .WithErrorCode("posting_profile_document_type_max_length");

        RuleForEach(x => x.Data.Lines)
            .ChildRules(line =>
            {
                line.RuleFor(l => l.AccountRole)
                    .NotEmpty()
                    .WithErrorCode("posting_profile_line_role_required")
                    .MaximumLength(50)
                    .WithErrorCode("posting_profile_line_role_max_length");

                line.RuleFor(l => l.AccountId)
                    .NotEmpty()
                    .WithErrorCode("posting_profile_line_account_required");
            });
    }
}
