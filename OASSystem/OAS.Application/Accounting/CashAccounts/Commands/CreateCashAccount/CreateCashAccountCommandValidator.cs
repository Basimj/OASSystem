using FluentValidation;

namespace OAS.Application.Accounting.CashAccounts.Commands.CreateCashAccount;

public sealed class CreateCashAccountCommandValidator
    : AbstractValidator<CreateCashAccountCommand>
{
    public CreateCashAccountCommandValidator()
    {
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
    }
}
