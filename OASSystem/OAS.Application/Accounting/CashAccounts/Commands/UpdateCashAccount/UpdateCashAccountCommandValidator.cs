using FluentValidation;

namespace OAS.Application.Accounting.CashAccounts.Commands.UpdateCashAccount;

public sealed class UpdateCashAccountCommandValidator
    : AbstractValidator<UpdateCashAccountCommand>
{
    public UpdateCashAccountCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("cash_account_id_required");

        RuleFor(x => x.Data.Code)
            .NotEmpty()
            .WithErrorCode("cash_account_code_required")
            .MaximumLength(30)
            .WithErrorCode("cash_account_code_max_length");

        RuleFor(x => x.Data.Name)
            .NotEmpty()
            .WithErrorCode("cash_account_name_required")
            .MaximumLength(150)
            .WithErrorCode("cash_account_name_max_length");

        RuleFor(x => x.Data.AccountId)
            .NotEmpty()
            .WithErrorCode("account_id_required");

        RuleFor(x => x.Data.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
