using FluentValidation;

namespace OAS.Application.Accounting.SupplierAccounts.Commands.CreateSupplierAccount;

public sealed class CreateSupplierAccountCommandValidator
    : AbstractValidator<CreateSupplierAccountCommand>
{
    public CreateSupplierAccountCommandValidator()
    {
        RuleFor(x => x.Data.SupplierId)
            .NotEmpty()
            .WithErrorCode("supplier_id_required");

        RuleFor(x => x.Data.AccountId)
            .NotEmpty()
            .WithErrorCode("account_id_required");

        RuleFor(x => x.Data.ControlAccountId)
            .NotEmpty()
            .WithErrorCode("control_account_id_required");
    }
}
