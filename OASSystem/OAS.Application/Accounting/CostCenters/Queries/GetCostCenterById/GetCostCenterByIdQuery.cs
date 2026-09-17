using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CostCenters.Queries.GetCostCenterById;

public sealed record GetCostCenterByIdQuery(Guid Id)
    : GetEntityByIdQuery<CostCenter, Guid, CostCenterDto>(Id),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CostCenters.View];
}
