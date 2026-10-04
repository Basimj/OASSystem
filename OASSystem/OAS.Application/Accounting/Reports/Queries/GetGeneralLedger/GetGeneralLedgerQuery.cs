using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetGeneralLedger;

public sealed record GetGeneralLedgerQuery(
    DateOnly FromDate,
    DateOnly ToDate,
    Guid? AccountId = null)
    : IQuery<GeneralLedgerReportDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [JournalPermissions.View];
}
