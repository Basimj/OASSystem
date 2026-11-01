using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.ReceiptVouchers.Mapping;
using OAS.Application.Accounting.ReceiptVouchers.Specifications;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.ReceiptVouchers.Queries.GetReceiptVouchers;

public sealed class GetReceiptVouchersQueryHandler(
    IReadRepository<ReceiptVoucher, Guid> repository,
    IReadRepository<ReceiptVoucherLine, Guid> lineRepository,
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

        // The generic paged repository does NOT load the Lines navigation.
        // Load the lines only for the voucher IDs on this page, not all vouchers.
        // Otherwise the list silently shows "جهة أخرى" and no currencies/amounts.
        var voucherIds = page.Items.Select(x => x.Id).ToArray();
        IReadOnlyList<ReceiptVoucherLine> lines = voucherIds.Length == 0
            ? Array.Empty<ReceiptVoucherLine>()
            : await lineRepository.ListAsync(
                new Specification<ReceiptVoucherLine>()
                    .Where(x => voucherIds.Contains(x.ReceiptVoucherId)),
                cancellationToken);
        var linesByVoucher = lines.ToLookup(x => x.ReceiptVoucherId);

        return new PagedResult<ReceiptVoucherDto>
        {
            Items = page.Items.Select(v => mapper.ToRead(v, linesByVoucher[v.Id])).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
