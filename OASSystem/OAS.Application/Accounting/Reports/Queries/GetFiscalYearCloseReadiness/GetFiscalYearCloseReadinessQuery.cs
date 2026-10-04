using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.FiscalYears.Authorization;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetFiscalYearCloseReadiness;

public sealed record GetFiscalYearCloseReadinessQuery(Guid FiscalYearId)
    : IQuery<FiscalCloseReadinessDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [FiscalYearPermissions.View];
}
