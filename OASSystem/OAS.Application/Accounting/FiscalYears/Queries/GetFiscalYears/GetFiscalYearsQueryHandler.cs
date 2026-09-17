using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.FiscalYears.Mapping;
using OAS.Application.Accounting.FiscalYears.Specifications;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalYears.Queries.GetFiscalYears;

public sealed class GetFiscalYearsQueryHandler(
    IReadRepository<FiscalYear, Guid> repository,
    FiscalYearMapper mapper)
    : IRequestHandler<GetFiscalYearsQuery, PagedResult<FiscalYearDto>>
{
    private static readonly FiscalYearPageSpecification SpecificationFactory = new();

    public async Task<PagedResult<FiscalYearDto>> Handle(
        GetFiscalYearsQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = SpecificationFactory.CreatePageSpecification(normalized);
        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<FiscalYearDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
