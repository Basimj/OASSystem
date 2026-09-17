using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.PaymentAllocations;

namespace OAS.Application.Accounting.PaymentAllocations.Commands.CreatePaymentAllocation;

public sealed record CreatePaymentAllocationCommand(CreatePaymentAllocationRequest Data)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.PaymentAllocations.Create];
}
