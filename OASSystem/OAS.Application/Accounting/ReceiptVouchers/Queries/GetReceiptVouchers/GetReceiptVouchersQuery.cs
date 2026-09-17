using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Contracts.Common.Pagination;

namespace OAS.Application.Accounting.ReceiptVouchers.Queries.GetReceiptVouchers;

public sealed record GetReceiptVouchersQuery(PageRequest Request)
    : IQuery<PagedResult<ReceiptVoucherDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.ReceiptVouchers.View];
}
