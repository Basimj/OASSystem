using FluentValidation;

namespace OAS.Application.Accounting.FiscalYears.Commands.SetFiscalYearStatus;

public sealed class SetFiscalYearStatusCommandValidator
    : AbstractValidator<SetFiscalYearStatusCommand>
{
    public SetFiscalYearStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("fiscal_year_id_required");

        RuleFor(x => x.Data.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}