using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetGeneralLedger;

public sealed record GetGeneralLedgerQuery(
    DateOnly FromDate,
    DateOnly ToDate,
    Guid? AccountId = null,
    Guid? FiscalYearId = null,
    Guid? FiscalPeriodId = null,
    bool IncludeAccountSubtree = false,
    JournalType? JournalType = null,
    string? SourceModule = null,
    Guid? CustomerId = null,
    Guid? SupplierId = null,
    string? Reference = null,
    bool IncludeOpeningBalance = true)
    : IQuery<GeneralLedgerReportDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [JournalPermissions.View];
}
