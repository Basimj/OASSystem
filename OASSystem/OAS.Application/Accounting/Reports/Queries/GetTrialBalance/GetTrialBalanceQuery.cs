using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetTrialBalance;

public sealed record GetTrialBalanceQuery(DateOnly FromDate, DateOnly ToDate)
    : IQuery<TrialBalanceReportDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [JournalPermissions.View];
}
