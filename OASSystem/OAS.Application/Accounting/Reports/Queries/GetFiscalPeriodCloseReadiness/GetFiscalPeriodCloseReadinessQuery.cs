using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.FiscalPeriods.Authorization;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetFiscalPeriodCloseReadiness;

public sealed record GetFiscalPeriodCloseReadinessQuery(Guid FiscalPeriodId)
    : IQuery<FiscalCloseReadinessDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [FiscalPeriodPermissions.View];
}
