using FluentValidation;

namespace OAS.Application.Accounting.Reports.Queries.GetJournalReport;

public sealed class GetJournalReportQueryValidator : AbstractValidator<GetJournalReportQuery>
{
    public GetJournalReportQueryValidator()
    {
        RuleFor(x => x.Filter.ToDate)
            .GreaterThanOrEqualTo(x => x.Filter.FromDate)
            .WithErrorCode("report_date_range_invalid");
        RuleFor(x => x.Filter.PageNumber)
            .GreaterThan(0)
            .WithErrorCode("page_number_invalid");
        RuleFor(x => x.Filter.PageSize)
            .InclusiveBetween(1, 500)
            .WithErrorCode("page_size_invalid");
        RuleFor(x => x.Filter.JournalType)
            .Must(value => !value.HasValue || Enum.IsDefined(value.Value))
            .WithErrorCode("report_journal_type_invalid");
        RuleFor(x => x.Filter.Status)
            .Must(value => !value.HasValue || Enum.IsDefined(value.Value))
            .WithErrorCode("report_journal_status_invalid");
        RuleFor(x => x.Filter.SourceModule)
            .MaximumLength(50)
            .WithErrorCode("report_source_module_max_length");
        RuleFor(x => x.Filter.JournalNumber)
            .MaximumLength(40)
            .WithErrorCode("report_journal_number_max_length");
        RuleFor(x => x.Filter.Reference)
            .MaximumLength(200)
            .WithErrorCode("report_reference_max_length");
    }
}
