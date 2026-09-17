using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.FiscalPeriods.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalPeriods.Queries.GetFiscalPeriodById;

public sealed record GetFiscalPeriodByIdQuery(
    Guid Id)
    : GetEntityByIdQuery<FiscalPeriod, Guid, FiscalPeriodDto>(Id),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [FiscalPeriodPermissions.View];
}