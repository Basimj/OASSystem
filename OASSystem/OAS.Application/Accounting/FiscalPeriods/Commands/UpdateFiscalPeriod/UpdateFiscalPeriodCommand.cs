using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.FiscalPeriods.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalPeriods.Commands.UpdateFiscalPeriod;

public sealed record UpdateFiscalPeriodCommand(
    Guid Id,
    UpdateFiscalPeriodRequest Data)
    : UpdateEntityCommand<
        FiscalPeriod,
        Guid,
        UpdateFiscalPeriodRequest>(Id, Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [FiscalPeriodPermissions.Edit];
}