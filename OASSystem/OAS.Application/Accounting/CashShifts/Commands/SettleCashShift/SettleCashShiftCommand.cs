using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.CashShifts;

namespace OAS.Application.Accounting.CashShifts.Commands.SettleCashShift;

public sealed record GetCashShiftSettlementAccountsQuery(Guid ShiftId) : IQuery<IReadOnlyList<CashShiftSettlementAccountDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.CashShifts.View, AccountingPermissions.Journals.Post];
}
public sealed record SettleCashShiftCommand(Guid ShiftId, CashShiftSettlementRequest Data) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.CashShifts.Approve, AccountingPermissions.Journals.Post];
}
public sealed class SettleCashShiftHandler(ICashShiftSettlementService service) :
    IRequestHandler<GetCashShiftSettlementAccountsQuery, IReadOnlyList<CashShiftSettlementAccountDto>>, IRequestHandler<SettleCashShiftCommand, Guid>
{
    public Task<IReadOnlyList<CashShiftSettlementAccountDto>> Handle(GetCashShiftSettlementAccountsQuery request, CancellationToken ct)
        => service.SettlementAccountsAsync(request.ShiftId, ct);
    public Task<Guid> Handle(SettleCashShiftCommand request, CancellationToken ct)
        => service.SettleAsync(request.ShiftId, request.Data, ct);
}
