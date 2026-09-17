using FluentValidation;

namespace OAS.Application.Accounting.Expenses.Commands.UpdateExpense;

public sealed class UpdateExpenseCommandValidator
    : AbstractValidator<UpdateExpenseCommand>
{
    public UpdateExpenseCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("expense_id_required");

        RuleFor(x => x.Data.ExpenseTypeId)
            .NotEmpty()
            .WithErrorCode("expense_type_id_required");

        RuleFor(x => x.Data.ExpenseAccountId)
            .NotEmpty()
            .WithErrorCode("expense_account_id_required");

        RuleFor(x => x.Data.Amount)
            .GreaterThan(0)
            .WithErrorCode("expense_amount_must_be_positive");

        RuleFor(x => x.Data.Beneficiary)
            .MaximumLength(200)
            .WithErrorCode("beneficiary_max_length")
            .When(x => !string.IsNullOrWhiteSpace(x.Data.Beneficiary));

        RuleFor(x => x.Data.Description)
            .MaximumLength(500)
            .WithErrorCode("description_max_length")
            .When(x => !string.IsNullOrWhiteSpace(x.Data.Description));

        RuleFor(x => x.Data.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
