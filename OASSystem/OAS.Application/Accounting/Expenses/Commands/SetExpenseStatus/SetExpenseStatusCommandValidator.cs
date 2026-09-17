using FluentValidation;

namespace OAS.Application.Accounting.Expenses.Commands.SetExpenseStatus;

public sealed class SetExpenseStatusCommandValidator
    : AbstractValidator<SetExpenseStatusCommand>
{
    public SetExpenseStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("expense_id_required");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
