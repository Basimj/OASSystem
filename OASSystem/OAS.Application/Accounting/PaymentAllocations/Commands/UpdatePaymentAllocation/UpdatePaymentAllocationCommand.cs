using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.PaymentAllocations;

namespace OAS.Application.Accounting.PaymentAllocations.Commands.UpdatePaymentAllocation;

public sealed record UpdatePaymentAllocationCommand(
    Guid Id,
    UpdatePaymentAllocationRequest Data)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.PaymentAllocations.Edit];
}
