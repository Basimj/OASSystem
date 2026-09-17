using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.FiscalPeriods.Mapping;
using OAS.Application.Accounting.FiscalPeriods.Specifications;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalPeriods.Queries.GetFiscalPeriods;

public sealed class GetFiscalPeriodsQueryHandler(
    IReadRepository<FiscalPeriod, Guid> repository,
    FiscalPeriodMapper mapper)
    : IRequestHandler<GetFiscalPeriodsQuery, PagedResult<FiscalPeriodDto>>
{
    public async Task<PagedResult<FiscalPeriodDto>> Handle(
        GetFiscalPeriodsQuery request,
        CancellationToken cancellationToken)
    {
        var spec = new FiscalPeriodPageSpecification(request.Request);
        var page = await repository.GetPageAsync(spec, cancellationToken);
        var normalized = request.Request.Normalize();

        return new PagedResult<FiscalPeriodDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
