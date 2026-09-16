using FluentValidation;

namespace OAS.Application.Accounting.Accounts.Commands.UpdateAccount;

public sealed class UpdateAccountCommandValidator
    : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.Data.Code)
            .NotEmpty()
            .WithErrorCode("account_code_required")
            .MaximumLength(30)
            .WithErrorCode("account_code_max_length");

        RuleFor(x => x.Data.NameAr)
            .NotEmpty()
            .WithErrorCode("account_name_ar_required")
            .MaximumLength(150)
            .WithErrorCode("account_name_ar_max_length");

        RuleFor(x => x.Data.NameEn)
            .MaximumLength(150)
            .WithErrorCode("account_name_en_max_length")
            .When(x => !string.IsNullOrWhiteSpace(x.Data.NameEn));

        RuleFor(x => x.Data.Level)
            .GreaterThan((byte)0)
            .WithErrorCode("account_level_invalid");

        RuleFor(x => x.Data.ParentAccountId)
            .Must(parentId => parentId is null || parentId != Guid.Empty)
            .WithErrorCode("parent_account_invalid");

        RuleFor(x => x.Data.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}