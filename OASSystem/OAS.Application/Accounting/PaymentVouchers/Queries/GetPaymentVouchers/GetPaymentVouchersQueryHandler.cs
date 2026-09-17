using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.PaymentVouchers.Mapping;
using OAS.Application.Accounting.PaymentVouchers.Specifications;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVouchers;

public sealed class GetPaymentVouchersQueryHandler(
    IReadRepository<PaymentVoucher, Guid> repository,
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

        return new PagedResult<PaymentVoucherDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
