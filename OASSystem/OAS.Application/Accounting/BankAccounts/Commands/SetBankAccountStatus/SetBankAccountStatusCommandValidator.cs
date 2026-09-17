using FluentValidation;

namespace OAS.Application.Accounting.BankAccounts.Commands.SetBankAccountStatus;

public sealed class SetBankAccountStatusCommandValidator
    : AbstractValidator<SetBankAccountStatusCommand>
{
    public SetBankAccountStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("bank_account_id_required");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
