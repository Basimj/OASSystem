using FluentValidation;

namespace OAS.Application.Accounting.FiscalPeriods.Commands.UpdateFiscalPeriod;

public sealed class UpdateFiscalPeriodCommandValidator
    : AbstractValidator<UpdateFiscalPeriodCommand>
{
    public UpdateFiscalPeriodCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("fiscal_period_id_required");

        RuleFor(x => x.Data.Name)
            .NotEmpty()
            .WithErrorCode("period_name_required")
            .MaximumLength(50)
            .WithErrorCode("period_name_max_length");

        RuleFor(x => x.Data.EndDate)
            .GreaterThanOrEqualTo(x => x.Data.StartDate)
            .WithErrorCode("period_dates_invalid");

        RuleFor(x => x.Data.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}