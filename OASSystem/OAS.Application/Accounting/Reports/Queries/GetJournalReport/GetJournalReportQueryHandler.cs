using MediatR;
using OAS.Application.Accounting.Abstractions;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetJournalReport;

public sealed class GetJournalReportQueryHandler(IAccountingReportingQueryService reports)
    : IRequestHandler<GetJournalReportQuery, JournalReportDto>
{
    public Task<JournalReportDto> Handle(GetJournalReportQuery request, CancellationToken cancellationToken) =>
        reports.GetJournalReportAsync(request.Filter, cancellationToken);
}
