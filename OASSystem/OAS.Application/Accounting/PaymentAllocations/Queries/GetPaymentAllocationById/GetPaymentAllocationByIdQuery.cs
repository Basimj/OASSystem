using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.PaymentAllocations;

namespace OAS.Application.Accounting.PaymentAllocations.Queries.GetPaymentAllocationById;

public sealed record GetPaymentAllocationByIdQuery(Guid Id)
    : IQuery<PaymentAllocationDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.PaymentAllocations.View];
}
