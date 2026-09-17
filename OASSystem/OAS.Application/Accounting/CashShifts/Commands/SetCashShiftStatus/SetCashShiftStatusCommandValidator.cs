using FluentValidation;

namespace OAS.Application.Accounting.CashShifts.Commands.SetCashShiftStatus;

public sealed class SetCashShiftStatusCommandValidator
    : AbstractValidator<SetCashShiftStatusCommand>
{
    public SetCashShiftStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("cash_shift_id_required");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");

        RuleFor(x => x.Request.Status)
            .IsInEnum()
            .WithErrorCode("invalid_cash_shift_status");
    }
}
