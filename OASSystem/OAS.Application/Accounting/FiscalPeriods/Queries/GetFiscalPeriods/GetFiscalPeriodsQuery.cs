using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.FiscalPeriods.Authorization;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Contracts.Common.Pagination;

namespace OAS.Application.Accounting.FiscalPeriods.Queries.GetFiscalPeriods;

public sealed record GetFiscalPeriodsQuery(
    PageRequest Request)
    : IQuery<PagedResult<FiscalPeriodDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [FiscalPeriodPermissions.View];
}