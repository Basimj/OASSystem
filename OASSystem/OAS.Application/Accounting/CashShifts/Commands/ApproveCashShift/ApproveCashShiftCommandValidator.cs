using FluentValidation;

namespace OAS.Application.Accounting.CashShifts.Commands.ApproveCashShift;

public sealed class ApproveCashShiftCommandValidator
    : AbstractValidator<ApproveCashShiftCommand>
{
    public ApproveCashShiftCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("cash_shift_id_required");

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
