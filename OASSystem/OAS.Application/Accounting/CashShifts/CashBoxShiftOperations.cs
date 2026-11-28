using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Accounting.CashShifts.Commands.CreateCashShift;
using OAS.Application.Accounting.CashShifts.Commands.CloseCashShift;
using OAS.Application.Accounting.CashShifts.Commands.ApproveCashShift;
using OAS.Application.Accounting.CashShifts.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.CashShifts;

public sealed record OpenCashBoxShiftCommand(OpenCashBoxShiftRequest Data) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.CashShifts.Create];
}
public sealed record CloseCashBoxShiftCommand(Guid Id, CloseCashBoxShiftRequest Data) : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.CashShifts.Close];
}
public sealed record ApproveCashBoxShiftCommand(Guid Id, ApproveCashBoxShiftRequest Data) : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.CashShifts.Approve];
}
public sealed record GetCashBoxShiftsQuery(Guid CashBoxId) : IQuery<IReadOnlyList<CashShiftDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.CashShifts.View];
}
public sealed class CashBoxShiftOperations(
    IReadRepository<CashAccount, Guid> accounts,
    IReadRepository<CashShift, Guid> shifts,
    IRequestHandler<CreateCashShiftCommand, Guid> open,
    IRequestHandler<CloseCashShiftCommand> close,
    IRequestHandler<ApproveCashShiftCommand> approve,
    CashShiftMapper mapper) : IRequestHandler<OpenCashBoxShiftCommand, Guid>, IRequestHandler<CloseCashBoxShiftCommand>,
    IRequestHandler<ApproveCashBoxShiftCommand>, IRequestHandler<GetCashBoxShiftsQuery, IReadOnlyList<CashShiftDto>>
{
    private static ConflictException Invalid(string message) => new("cash_box_shift_invalid", message);
    private static bool Exact(IEnumerable<Guid> expected, IEnumerable<Guid> supplied)
    {
        var ids = supplied.ToArray();
        return ids.Length == ids.Distinct().Count() && expected.ToHashSet().SetEquals(ids);
    }
    public async Task<Guid> Handle(OpenCashBoxShiftCommand request, CancellationToken ct)
    {
        var data = request.Data;
        var members = await accounts.ListAsync(new Specification<CashAccount>().Where(x => x.CashBoxId == data.CashBoxId && x.IsActive), ct);
        if (members.Count == 0 || members.Any(x => !x.CurrencyId.HasValue) || data.Currencies is null ||
            !Exact(members.Select(x => x.Id), data.Currencies.Select(x => x.CashAccountId)))
            throw Invalid("يجب إدخال جرد افتتاحي لكل عملة نشطة في الصندوق.");
        if (data.Currencies.Any(x => x.OpeningBalance < 0 || x.OpeningDifferenceReason?.Length > 500 || !x.ExpectedBookOpeningBalance.HasValue))
            throw Invalid("راجع مبالغ الافتتاح وأسباب الفروقات وحدّث الأرصدة الدفترية.");
        var group = Guid.NewGuid();
        foreach (var line in data.Currencies.OrderBy(x => x.CashAccountId))
            await open.Handle(new CreateCashShiftCommand(line with { ShiftNumber = null }) { GroupId = group }, ct);
        return group;
    }
    private Task<IReadOnlyList<CashShift>> Members(Guid id, CancellationToken ct) =>
        shifts.ListAsync(new Specification<CashShift>().Where(x => x.CashBoxShiftId == id), ct);
    public async Task Handle(CloseCashBoxShiftCommand request, CancellationToken ct)
    {
        var members = await Members(request.Id, ct);
        var lines = request.Data.Currencies;
        if (members.Count == 0 || lines is null || !Exact(members.Select(x => x.Id), lines.Select(x => x.ShiftId)) ||
            members.Any(x => x.Status is not (CashShiftStatus.Open or CashShiftStatus.Closing)) ||
            lines.Any(x => x.ActualClosingBalance is null or < 0 || string.IsNullOrWhiteSpace(x.RowVersion)))
            throw Invalid("أدخل الجرد الفعلي لجميع عملات الوردية المفتوحة.");
        foreach (var line in lines.OrderBy(x => x.ShiftId))
            await close.Handle(new CloseCashShiftCommand(line.ShiftId, new(line.ActualClosingBalance, line.RowVersion)) { GroupId = request.Id }, ct);
    }
    public async Task Handle(ApproveCashBoxShiftCommand request, CancellationToken ct)
    {
        var members = await Members(request.Id, ct);
        var lines = request.Data.Currencies;
        if (members.Count == 0 || lines is null || !Exact(members.Select(x => x.Id), lines.Select(x => x.ShiftId)) ||
            members.Any(x => x.Status != CashShiftStatus.Closed) || lines.Any(x => string.IsNullOrWhiteSpace(x.RowVersion)))
            throw Invalid("يجب إغلاق جميع عملات الوردية قبل الاعتماد.");
        foreach (var line in lines.OrderBy(x => x.ShiftId))
            await approve.Handle(new ApproveCashShiftCommand(line.ShiftId, line.RowVersion) { GroupId = request.Id }, ct);
    }
    public async Task<IReadOnlyList<CashShiftDto>> Handle(GetCashBoxShiftsQuery request, CancellationToken ct)
    {
        var members = await accounts.ListAsync(new Specification<CashAccount>().Where(x => x.CashBoxId == request.CashBoxId), ct);
        var ids = members.Select(x => x.Id).ToArray();
        var rows = await shifts.ListAsync(new Specification<CashShift>().Where(x => ids.Contains(x.CashAccountId)), ct);
        // Return complete groups, never truncate currency members independently.
        return rows.OrderByDescending(x => x.OpenedAtUtc).Select(mapper.ToRead).ToArray();
    }
}
