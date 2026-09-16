using FluentValidation;

namespace OAS.Application.Accounting.Accounts.Commands.SetAccountStatus;

public sealed class SetAccountStatusCommandValidator
    : AbstractValidator<SetAccountStatusCommand>
{
    public SetAccountStatusCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithErrorCode("account_id_required");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}