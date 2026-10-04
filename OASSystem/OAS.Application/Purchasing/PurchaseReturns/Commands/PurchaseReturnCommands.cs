using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Purchasing.PurchaseReturns;

namespace OAS.Application.Purchasing.PurchaseReturns.Commands;

public sealed record CreatePurchaseReturnCommand(CreatePurchaseReturnRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Receipts.Create];
}
public sealed record ConfirmPurchaseReturnCommand(Guid Id, PurchaseReturnActionRequest Request) : ICommand<PurchaseReturnDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Receipts.Confirm];
}
public sealed record PostPurchaseReturnCommand(Guid Id, PurchaseReturnActionRequest Request) : ICommand<PurchaseReturnPostingResultDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Receipts.Post];
}
public sealed record CancelPurchaseReturnCommand(Guid Id, CancelPurchaseReturnRequest Request) : ICommand<PurchaseReturnDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Receipts.Cancel];
}
