using FluentValidation;

namespace OAS.Application.Accounting.FiscalYears.Commands.UpdateFiscalYear;

public sealed class UpdateFiscalYearCommandValidator
    : AbstractValidator<UpdateFiscalYearCommand>
{
    public UpdateFiscalYearCommandValidator()
    {
        RuleFor(x => x.Data.Code)
            .NotEmpty()
            .WithErrorCode("fiscal_year_code_required")
            .MaximumLength(20)
            .WithErrorCode("fiscal_year_code_max_length");

        RuleFor(x => x.Data.Name)
            .NotEmpty()
            .WithErrorCode("fiscal_year_name_required")
            .MaximumLength(100)
            .WithErrorCode("fiscal_year_name_max_length");

        RuleFor(x => x.Data)
            .Must(x => x.StartDate <= x.EndDate)
            .WithErrorCode("fiscal_year_invalid_date_range")
            .WithMessage(
                "Fiscal year start date must be before or equal to end date.");

        RuleFor(x => x.Data.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}