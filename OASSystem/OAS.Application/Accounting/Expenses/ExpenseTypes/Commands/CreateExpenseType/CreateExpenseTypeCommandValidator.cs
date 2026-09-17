using FluentValidation;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Commands.CreateExpenseType;

public sealed class CreateExpenseTypeCommandValidator
    : AbstractValidator<CreateExpenseTypeCommand>
{
    public CreateExpenseTypeCommandValidator()
    {
        RuleFor(x => x.Data.Code)
            .NotEmpty()
            .WithErrorCode("expense_type_code_required")
            .MaximumLength(30)
            .WithErrorCode("expense_type_code_max_length");

        RuleFor(x => x.Data.NameAr)
            .NotEmpty()
            .WithErrorCode("expense_type_name_ar_required")
            .MaximumLength(150)
            .WithErrorCode("expense_type_name_ar_max_length");

        RuleFor(x => x.Data.NameEn)
            .MaximumLength(150)
            .WithErrorCode("expense_type_name_en_max_length")
            .When(x => !string.IsNullOrWhiteSpace(x.Data.NameEn));
    }
}
