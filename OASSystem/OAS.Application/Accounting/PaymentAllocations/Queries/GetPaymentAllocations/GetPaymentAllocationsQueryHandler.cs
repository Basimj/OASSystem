using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.PaymentAllocations.Mapping;
using OAS.Application.Accounting.PaymentAllocations.Specifications;
using OAS.Contracts.Accounting.PaymentAllocations;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PaymentAllocations.Queries.GetPaymentAllocations;

public sealed class GetPaymentAllocationsQueryHandler(
    IReadRepository<PaymentAllocation, Guid> repository,
    PaymentAllocationMapper mapper)
    : IRequestHandler<GetPaymentAllocationsQuery, PagedResult<PaymentAllocationDto>>
{
    private static readonly PaymentAllocationPageSpecification SpecificationFactory = new();

    public async Task<PagedResult<PaymentAllocationDto>> Handle(
        GetPaymentAllocationsQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = SpecificationFactory.CreatePageSpecification(normalized);
        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<PaymentAllocationDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
