using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CostCenters.Queries.GetCostCenters;

public sealed record GetCostCentersQuery(PageRequest Request)
    : GetEntityPageQuery<CostCenter, Guid, CostCenterDto>(Request),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CostCenters.View];
}
