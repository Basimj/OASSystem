using FluentValidation;

namespace OAS.Application.Accounting.CashAccounts.Commands.SetCashAccountStatus;

public sealed class SetCashAccountStatusCommandValidator
    : AbstractValidator<SetCashAccountStatusCommand>
{
    public SetCashAccountStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("cash_account_id_required");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
