using FluentValidation;

namespace OAS.Application.Accounting.FiscalPeriods.Commands.SetFiscalPeriodLocks;

public sealed class SetFiscalPeriodLocksCommandValidator
    : AbstractValidator<SetFiscalPeriodLocksCommand>
{
    public SetFiscalPeriodLocksCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("fiscal_period_id_required");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}