using FluentValidation;

namespace OAS.Application.Accounting.Reports.Queries.GetGeneralLedger;

public sealed class GetGeneralLedgerQueryValidator : AbstractValidator<GetGeneralLedgerQuery>
{
    public GetGeneralLedgerQueryValidator()
    {
        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithErrorCode("report_date_range_invalid");
        RuleFor(x => x.SourceModule)
            .MaximumLength(50)
            .WithErrorCode("report_source_module_max_length");
        RuleFor(x => x.Reference)
            .MaximumLength(200)
            .WithErrorCode("report_reference_max_length");
        RuleFor(x => x.JournalType)
            .Must(value => !value.HasValue || Enum.IsDefined(value.Value))
            .WithErrorCode("report_journal_type_invalid");
    }
}
