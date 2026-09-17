using FluentValidation;

namespace OAS.Application.Accounting.CashShifts.Commands.CreateCashShift;

public sealed class CreateCashShiftCommandValidator
    : AbstractValidator<CreateCashShiftCommand>
{
    public CreateCashShiftCommandValidator()
    {
        RuleFor(x => x.Data.CashAccountId)
            .NotEmpty()
            .WithErrorCode("cash_account_id_required");

        RuleFor(x => x.Data.OpeningBalance)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("opening_balance_must_be_non_negative");
    }
}
