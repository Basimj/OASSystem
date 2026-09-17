using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.CostCenters.Mapping;
using OAS.Application.Accounting.CostCenters.Specifications;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CostCenters.Queries.GetCostCenters;

public sealed class GetCostCentersQueryHandler(
    IReadRepository<CostCenter, Guid> repository,
    CostCenterMapper mapper)
    : IRequestHandler<GetCostCentersQuery, PagedResult<CostCenterDto>>
{
    private static readonly CostCenterPageSpecification SpecificationFactory = new();

    public async Task<PagedResult<CostCenterDto>> Handle(
        GetCostCentersQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = SpecificationFactory.CreatePageSpecification(normalized);
        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<CostCenterDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
