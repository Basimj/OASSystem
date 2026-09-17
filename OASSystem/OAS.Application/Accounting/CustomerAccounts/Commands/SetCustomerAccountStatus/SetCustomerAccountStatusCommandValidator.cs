using FluentValidation;

namespace OAS.Application.Accounting.CustomerAccounts.Commands.SetCustomerAccountStatus;

public sealed class SetCustomerAccountStatusCommandValidator
    : AbstractValidator<SetCustomerAccountStatusCommand>
{
    public SetCustomerAccountStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("customer_account_id_required");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
