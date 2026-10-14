using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetJournalReport;

public sealed record GetJournalReportQuery(JournalReportFilterDto Filter)
    : IQuery<JournalReportDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [JournalPermissions.View];
}
