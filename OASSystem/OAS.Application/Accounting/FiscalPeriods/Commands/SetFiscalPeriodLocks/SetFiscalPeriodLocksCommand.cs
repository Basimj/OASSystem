using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.FiscalPeriods.Authorization;
using OAS.Contracts.Accounting.FiscalPeriods;

namespace OAS.Application.Accounting.FiscalPeriods.Commands.SetFiscalPeriodLocks;

public sealed record SetFiscalPeriodLocksCommand(
    Guid Id,
    SetFiscalPeriodLocksRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [FiscalPeriodPermissions.Locks];
}