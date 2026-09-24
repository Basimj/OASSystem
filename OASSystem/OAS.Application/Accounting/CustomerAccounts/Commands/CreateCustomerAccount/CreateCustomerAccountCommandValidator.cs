using FluentValidation;

namespace OAS.Application.Accounting.CustomerAccounts.Commands.CreateCustomerAccount;

public sealed class CreateCustomerAccountCommandValidator
    : AbstractValidator<CreateCustomerAccountCommand>
{
    public CreateCustomerAccountCommandValidator()
    {
        RuleFor(x => x.Data.CustomerId)
            .NotEmpty()
            .WithErrorCode("customer_id_required");

        RuleFor(x => x.Data.AccountId)
            .NotEmpty()
            .WithErrorCode("account_id_required");

        RuleFor(x => x.Data.ControlAccountId)
            .NotEmpty()
            .WithErrorCode("control_account_id_required");
    }
}
