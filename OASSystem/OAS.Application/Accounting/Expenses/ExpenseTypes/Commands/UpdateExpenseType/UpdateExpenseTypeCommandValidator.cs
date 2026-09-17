using FluentValidation;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Commands.UpdateExpenseType;

public sealed class UpdateExpenseTypeCommandValidator
    : AbstractValidator<UpdateExpenseTypeCommand>
{
    public UpdateExpenseTypeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("expense_type_id_required");

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

        RuleFor(x => x.Data.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
