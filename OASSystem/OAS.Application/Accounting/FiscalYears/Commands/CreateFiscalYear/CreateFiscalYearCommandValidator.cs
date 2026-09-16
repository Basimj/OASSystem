using FluentValidation;

namespace OAS.Application.Accounting.FiscalYears.Commands.CreateFiscalYear;

public sealed class CreateFiscalYearCommandValidator
    : AbstractValidator<CreateFiscalYearCommand>
{
    public CreateFiscalYearCommandValidator()
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
    }
}