using FluentValidation;

namespace OAS.Application.Accounting.CustomerAccounts.Commands.UpdateCustomerAccount;

public sealed class UpdateCustomerAccountCommandValidator
    : AbstractValidator<UpdateCustomerAccountCommand>
{
    public UpdateCustomerAccountCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("customer_account_id_required");

        RuleFor(x => x.Data.AccountId)
            .NotEmpty()
            .WithErrorCode("account_id_required");

        RuleFor(x => x.Data.ControlAccountId)
            .NotEmpty()
            .WithErrorCode("control_account_id_required");

        RuleFor(x => x.Data.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
