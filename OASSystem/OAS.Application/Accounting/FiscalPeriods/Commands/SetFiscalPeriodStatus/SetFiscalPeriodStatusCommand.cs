using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.FiscalPeriods.Authorization;
using OAS.Contracts.Accounting.FiscalPeriods;

namespace OAS.Application.Accounting.FiscalPeriods.Commands.SetFiscalPeriodStatus;

public sealed record SetFiscalPeriodStatusCommand(
    Guid Id,
    SetFiscalPeriodStatusRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [FiscalPeriodPermissions.Status];
}