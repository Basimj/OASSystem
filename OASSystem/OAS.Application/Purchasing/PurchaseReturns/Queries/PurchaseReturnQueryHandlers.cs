using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.PurchaseReturns;

namespace OAS.Application.Purchasing.PurchaseReturns.Queries;

public sealed class GetPurchaseReturnByIdQueryHandler(IPurchaseReturnRepository repository)
    : IRequestHandler<GetPurchaseReturnByIdQuery, PurchaseReturnDto>
{
    public async Task<PurchaseReturnDto> Handle(GetPurchaseReturnByIdQuery request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("PurchaseReturn", request.Id);
        return PurchaseReturnMapper.ToDto(entity);
    }
}

public sealed class GetPurchaseReturnsQueryHandler(IPurchaseReturnRepository repository)
    : IRequestHandler<GetPurchaseReturnsQuery, PagedResult<PurchaseReturnDto>>
{
    public async Task<PagedResult<PurchaseReturnDto>> Handle(GetPurchaseReturnsQuery request, CancellationToken ct)
    {
        var page = await repository.GetPageAsync(request.Request, request.Status, request.PurchaseReceiptId,
            request.PurchaseInvoiceId, request.SupplierId, ct);
        return PurchaseReturnMapper.ToPage(page, request.Request);
    }
}
