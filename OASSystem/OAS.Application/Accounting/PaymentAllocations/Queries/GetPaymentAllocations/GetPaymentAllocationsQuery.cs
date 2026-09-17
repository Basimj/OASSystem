using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.PaymentAllocations;
using OAS.Contracts.Common.Pagination;

namespace OAS.Application.Accounting.PaymentAllocations.Queries.GetPaymentAllocations;

public sealed record GetPaymentAllocationsQuery(PageRequest Request)
    : IQuery<PagedResult<PaymentAllocationDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.PaymentAllocations.View];
}
