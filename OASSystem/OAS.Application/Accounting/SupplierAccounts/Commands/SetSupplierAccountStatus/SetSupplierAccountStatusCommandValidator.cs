using FluentValidation;

namespace OAS.Application.Accounting.SupplierAccounts.Commands.SetSupplierAccountStatus;

public sealed class SetSupplierAccountStatusCommandValidator
    : AbstractValidator<SetSupplierAccountStatusCommand>
{
    public SetSupplierAccountStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("supplier_account_id_required");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
