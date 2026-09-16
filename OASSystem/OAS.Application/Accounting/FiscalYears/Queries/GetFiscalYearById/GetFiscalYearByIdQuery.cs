using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.FiscalYears.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalYears.Queries.GetFiscalYearById;

public sealed record GetFiscalYearByIdQuery(
    Guid Id)
    : GetEntityByIdQuery<
        FiscalYear,
        Guid,
        FiscalYearDto>(Id),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [FiscalYearPermissions.View];
}