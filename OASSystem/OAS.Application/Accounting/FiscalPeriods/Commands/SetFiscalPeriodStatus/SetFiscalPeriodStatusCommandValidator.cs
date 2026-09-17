using FluentValidation;
using OAS.Contracts.Accounting.Enums;

namespace OAS.Application.Accounting.FiscalPeriods.Commands.SetFiscalPeriodStatus;

public sealed class SetFiscalPeriodStatusCommandValidator
    : AbstractValidator<SetFiscalPeriodStatusCommand>
{
    public SetFiscalPeriodStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("fiscal_period_id_required");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");

        RuleFor(x => x.Request.Status)
            .IsInEnum()
            .WithErrorCode("invalid_fiscal_period_status");
    }
}