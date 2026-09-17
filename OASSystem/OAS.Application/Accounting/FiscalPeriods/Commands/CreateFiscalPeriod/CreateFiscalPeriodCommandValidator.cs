using FluentValidation;

namespace OAS.Application.Accounting.FiscalPeriods.Commands.CreateFiscalPeriod;

public sealed class CreateFiscalPeriodCommandValidator
    : AbstractValidator<CreateFiscalPeriodCommand>
{
    public CreateFiscalPeriodCommandValidator()
    {
        RuleFor(x => x.Data.FiscalYearId)
            .NotEmpty()
            .WithErrorCode("fiscal_year_required");

        RuleFor(x => x.Data.PeriodNumber)
            .GreaterThan((byte)0)
            .WithErrorCode("period_number_required");

        RuleFor(x => x.Data.Name)
            .NotEmpty()
            .WithErrorCode("period_name_required")
            .MaximumLength(50)
            .WithErrorCode("period_name_max_length");

        RuleFor(x => x.Data.EndDate)
            .GreaterThanOrEqualTo(x => x.Data.StartDate)
            .WithErrorCode("period_dates_invalid");
    }
}