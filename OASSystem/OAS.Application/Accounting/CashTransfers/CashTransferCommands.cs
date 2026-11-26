using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.CashTransfers;

namespace OAS.Application.Accounting.CashTransfers;

// Use existing journal permissions for the equivalent financial action; no implicit grants.
public sealed record GetCashTransferLookupsQuery(DateOnly? Date = null) : IQuery<CashTransferLookupsDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.Journals.View];
}
public sealed record GetCashTransfersQuery(int Page, string? Search, string? Type = null, int? Status = null) : IQuery<CashTransferPageDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.Journals.View];
}
public sealed record SaveCashTransferCommand(Guid? Id, CashTransferRequest Data) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => [Id.HasValue ? AccountingPermissions.Journals.Edit : AccountingPermissions.Journals.Create];
}
public sealed record CashTransferActionCommand(Guid Id, string Action, CashTransferActionRequest Data) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => [Action switch
    {
        "approve" => AccountingPermissions.Journals.Approve,
        "post" => AccountingPermissions.Journals.Post,
        "reverse" => AccountingPermissions.Journals.Reverse,
        "cancel" => AccountingPermissions.Journals.Edit,
        _ => throw new ConflictException("cash_transfer_action_invalid", "الإجراء غير صالح.")
    }];
}
public sealed class CashTransferHandler(ICashTransferService service) :
    IRequestHandler<GetCashTransferLookupsQuery, CashTransferLookupsDto>, IRequestHandler<GetCashTransfersQuery, CashTransferPageDto>,
    IRequestHandler<SaveCashTransferCommand, Guid>, IRequestHandler<CashTransferActionCommand, Guid>
{
    public Task<CashTransferLookupsDto> Handle(GetCashTransferLookupsQuery request, CancellationToken ct) => service.LookupsAsync(ct, request.Date);
    public Task<CashTransferPageDto> Handle(GetCashTransfersQuery request, CancellationToken ct) => service.ListAsync(request.Page, request.Search, ct, request.Type, request.Status);
    public Task<Guid> Handle(SaveCashTransferCommand request, CancellationToken ct) => service.SaveAsync(request.Id, request.Data, ct);
    public Task<Guid> Handle(CashTransferActionCommand request, CancellationToken ct) => service.ActionAsync(request.Id, request.Action, request.Data, ct);
}
