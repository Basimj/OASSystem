using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Common.Pagination;

namespace OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVouchers;

public sealed record GetPaymentVouchersQuery(PageRequest Request)
    : IQuery<PagedResult<PaymentVoucherDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.PaymentVouchers.View];
}
