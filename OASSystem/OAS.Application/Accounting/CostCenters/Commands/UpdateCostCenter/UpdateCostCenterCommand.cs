using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CostCenters.Commands.UpdateCostCenter;

public sealed record UpdateCostCenterCommand(Guid Id, UpdateCostCenterRequest Data)
    : UpdateEntityCommand<CostCenter, Guid, UpdateCostCenterRequest>(Id, Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CostCenters.Edit];
}
