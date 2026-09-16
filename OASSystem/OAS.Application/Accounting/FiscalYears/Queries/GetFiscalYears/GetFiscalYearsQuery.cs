using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.FiscalYears.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalYears.Queries.GetFiscalYears;

public sealed record GetFiscalYearsQuery(
    PageRequest Request)
    : GetEntityPageQuery<
        FiscalYear,
        Guid,
        FiscalYearDto>(Request),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [FiscalYearPermissions.View];
}