using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.CashShifts.Mapping;
using OAS.Application.Accounting.CashShifts.Specifications;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashShifts.Queries.GetCashShifts;

public sealed class GetCashShiftsQueryHandler(
    IReadRepository<CashShift, Guid> repository,
    CashShiftMapper mapper)
    : IRequestHandler<GetCashShiftsQuery, PagedResult<CashShiftDto>>
{
    private static readonly CashShiftPageSpecification SpecificationFactory = new();

    public async Task<PagedResult<CashShiftDto>> Handle(
        GetCashShiftsQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = SpecificationFactory.CreatePageSpecification(normalized);
        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<CashShiftDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
