using FluentValidation;

namespace OAS.Application.Accounting.SupplierAccounts.Commands.UpdateSupplierAccount;

public sealed class UpdateSupplierAccountCommandValidator
    : AbstractValidator<UpdateSupplierAccountCommand>
{
    public UpdateSupplierAccountCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("supplier_account_id_required");

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
