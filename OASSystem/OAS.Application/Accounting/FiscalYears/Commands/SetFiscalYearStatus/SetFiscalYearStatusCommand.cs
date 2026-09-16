using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.FiscalYears.Authorization;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalYears.Commands.SetFiscalYearStatus;

public sealed record SetFiscalYearStatusCommand(
    Guid Id,
    SetFiscalYearStatusRequest Data)
    : ICommand,
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [FiscalYearPermissions.Status];
}