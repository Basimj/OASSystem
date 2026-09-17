using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.FiscalPeriods.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalPeriods.Commands.CreateFiscalPeriod;

public sealed record CreateFiscalPeriodCommand(
    CreateFiscalPeriodRequest Data)
    : CreateEntityCommand<
        FiscalPeriod,
        Guid,
        CreateFiscalPeriodRequest>(Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [FiscalPeriodPermissions.Create];
}