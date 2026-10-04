using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Returns;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Returns.Queries;

public sealed class GetSalesReturnByIdQueryHandler(ISalesReturnQueryService query)
    : IRequestHandler<GetSalesReturnByIdQuery, SalesReturnDto>
{
    public async Task<SalesReturnDto> Handle(GetSalesReturnByIdQuery request, CancellationToken ct) =>
        await query.GetByIdAsync(request.Id, ct) ?? throw new NotFoundException(nameof(SalesReturn), request.Id);
}

public sealed class GetSalesReturnsQueryHandler(ISalesReturnQueryService query)
    : IRequestHandler<GetSalesReturnsQuery, PagedResult<SalesReturnDto>>
{
    public Task<PagedResult<SalesReturnDto>> Handle(GetSalesReturnsQuery request, CancellationToken ct) =>
        query.GetPageAsync(request.Request, request.SalesInvoiceId, request.CustomerId, request.Status, ct);
}
