using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.ReceiptVouchers.Mapping;
using OAS.Application.Accounting.ReceiptVouchers.Specifications;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.ReceiptVouchers.Queries.GetReceiptVouchers;

public sealed class GetReceiptVouchersQueryHandler(
    IReadRepository<ReceiptVoucher, Guid> repository,
    ReceiptVoucherMapper mapper)
    : IRequestHandler<GetReceiptVouchersQuery, PagedResult<ReceiptVoucherDto>>
{
    private static readonly ReceiptVoucherPageSpecification SpecificationFactory = new();

    public async Task<PagedResult<ReceiptVoucherDto>> Handle(
        GetReceiptVouchersQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = SpecificationFactory.CreatePageSpecification(normalized);
        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<ReceiptVoucherDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
