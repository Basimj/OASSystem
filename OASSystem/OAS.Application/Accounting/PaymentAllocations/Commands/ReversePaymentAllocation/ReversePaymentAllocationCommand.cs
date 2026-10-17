using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.PaymentAllocations;

namespace OAS.Application.Accounting.PaymentAllocations.Commands.ReversePaymentAllocation;

public sealed record ReversePaymentAllocationCommand(Guid Id, ReversePaymentAllocationRequest Data)
    : ICommand<ReversePaymentAllocationResultDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
    [
        AccountingPermissions.PaymentAllocations.Edit,
        AccountingPermissions.Journals.Reverse
    ];
}
