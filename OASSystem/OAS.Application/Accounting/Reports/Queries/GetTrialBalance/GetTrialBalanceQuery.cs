using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetTrialBalance;

public sealed record GetTrialBalanceQuery(
    DateOnly FromDate,
    DateOnly ToDate,
    DateOnly? AsOfDate = null,
    Guid? FiscalYearId = null,
    Guid? FiscalPeriodId = null,
    byte? AccountLevel = null,
    bool PostingAccountsOnly = false,
    bool IncludeZeroBalance = false,
    bool GroupedTree = false)
    : IQuery<TrialBalanceReportDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [JournalPermissions.View];
}
