using FluentValidation;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Commands.SetExpenseTypeStatus;

public sealed class SetExpenseTypeStatusCommandValidator
    : AbstractValidator<SetExpenseTypeStatusCommand>
{
    public SetExpenseTypeStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("expense_type_id_required");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
