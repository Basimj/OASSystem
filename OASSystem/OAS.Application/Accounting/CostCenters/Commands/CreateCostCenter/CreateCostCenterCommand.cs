using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CostCenters.Commands.CreateCostCenter;

public sealed record CreateCostCenterCommand(CreateCostCenterRequest Data)
    : CreateEntityCommand<CostCenter, Guid, CreateCostCenterRequest>(Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CostCenters.Create];
}
