using FluentValidation;

namespace OAS.Application.Accounting.BankAccounts.Commands.UpdateBankAccount;

public sealed class UpdateBankAccountCommandValidator
    : AbstractValidator<UpdateBankAccountCommand>
{
    public UpdateBankAccountCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("bank_account_id_required");

        RuleFor(x => x.Data.BankName)
            .NotEmpty()
            .WithErrorCode("bank_name_required")
            .MaximumLength(150)
            .WithErrorCode("bank_name_max_length");

        RuleFor(x => x.Data.AccountName)
            .NotEmpty()
            .WithErrorCode("account_name_required")
            .MaximumLength(150)
            .WithErrorCode("account_name_max_length");

        RuleFor(x => x.Data.AccountNumber)
            .NotEmpty()
            .WithErrorCode("account_number_required")
            .MaximumLength(100)
            .WithErrorCode("account_number_max_length");

        RuleFor(x => x.Data.IBAN)
            .MaximumLength(50)
            .WithErrorCode("iban_max_length")
            .When(x => !string.IsNullOrWhiteSpace(x.Data.IBAN));

        RuleFor(x => x.Data.CurrencyId)
            .NotEmpty()
            .WithErrorCode("currency_id_required");

        RuleFor(x => x.Data.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
