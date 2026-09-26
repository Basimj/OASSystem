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

        RuleFor(x => x.Data.Name)
            .NotEmpty()
            .WithErrorCode("cash_account_name_required")
            .MaximumLength(150)
            .WithErrorCode("cash_account_name_max_length");

        RuleFor(x => x.Data.CurrencyId)
            .NotEmpty()
            .WithErrorCode("currency_id_required");

        RuleFor(x => x.Data.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
