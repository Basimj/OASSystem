using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Purchasing.PurchaseReceipts;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.PurchaseReceipts.Queries;

public sealed record GetPurchaseReceiptFollowUpsQuery(Guid? ReceiptId, ReceiptFollowUpStatus? Status, int PageNumber = 1, int PageSize = 50)
    : IQuery<PurchaseReceiptFollowUpPageDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Receipts.View];
}
public sealed class GetPurchaseReceiptFollowUpsQueryHandler(IPurchaseReceiptFollowUpJobs jobs)
    : IRequestHandler<GetPurchaseReceiptFollowUpsQuery, PurchaseReceiptFollowUpPageDto>
{
    public Task<PurchaseReceiptFollowUpPageDto> Handle(GetPurchaseReceiptFollowUpsQuery request, CancellationToken ct) =>
        jobs.ListAsync(request.ReceiptId, request.Status, request.PageNumber, request.PageSize, ct);
}
public sealed record RetryPurchaseReceiptFollowUpCommand(Guid Id) : ICommand<bool>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Receipts.Post];
}
public sealed class RetryPurchaseReceiptFollowUpCommandHandler(IPurchaseReceiptFollowUpJobs jobs)
    : IRequestHandler<RetryPurchaseReceiptFollowUpCommand, bool>
{
    public Task<bool> Handle(RetryPurchaseReceiptFollowUpCommand request, CancellationToken ct) => jobs.RetryAsync(request.Id, ct);
}
