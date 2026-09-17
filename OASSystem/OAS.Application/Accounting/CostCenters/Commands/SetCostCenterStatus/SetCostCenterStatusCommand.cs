using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.CostCenters;

namespace OAS.Application.Accounting.CostCenters.Commands.SetCostCenterStatus;

public sealed record SetCostCenterStatusCommand(Guid Id, SetCostCenterStatusRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CostCenters.Disable];
}
