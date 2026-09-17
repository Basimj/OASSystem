using FluentValidation;

namespace OAS.Application.Accounting.CashShifts.Commands.CloseCashShift;

public sealed class CloseCashShiftCommandValidator
    : AbstractValidator<CloseCashShiftCommand>
{
    public CloseCashShiftCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("cash_shift_id_required");

        RuleFor(x => x.Request.ActualClosingBalance)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("actual_closing_balance_must_be_non_negative");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
