using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.PaymentVouchers.Mapping;
using OAS.Application.Accounting.PaymentVouchers.Specifications;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVouchers;

public sealed class GetPaymentVouchersQueryHandler(
    IReadRepository<PaymentVoucher, Guid> repository,
    IReadRepository<PaymentVoucherLine, Guid> lineRepository,
    PaymentVoucherMapper mapper)
    : IRequestHandler<GetPaymentVouchersQuery, PagedResult<PaymentVoucherDto>>
{
    private static readonly PaymentVoucherPageSpecification SpecificationFactory = new();

    public async Task<PagedResult<PaymentVoucherDto>> Handle(
        GetPaymentVouchersQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = SpecificationFactory.CreatePageSpecification(normalized);
        var page = await repository.GetPageAsync(spec, cancellationToken);

        // The generic paged repository does NOT load the Lines navigation.
        // Load the lines only for the voucher IDs on this page, not all vouchers.
        // Otherwise the list silently shows "جهة أخرى" and no currencies/amounts.
        var voucherIds = page.Items.Select(x => x.Id).ToArray();
        IReadOnlyList<PaymentVoucherLine> lines = voucherIds.Length == 0
            ? Array.Empty<PaymentVoucherLine>()
            : await lineRepository.ListAsync(
                new Specification<PaymentVoucherLine>()
                    .Where(x => voucherIds.Contains(x.PaymentVoucherId)),
                cancellationToken);
        var linesByVoucher = lines.ToLookup(x => x.PaymentVoucherId);

        return new PagedResult<PaymentVoucherDto>
        {
            Items = page.Items.Select(v => mapper.ToRead(v, linesByVoucher[v.Id])).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
