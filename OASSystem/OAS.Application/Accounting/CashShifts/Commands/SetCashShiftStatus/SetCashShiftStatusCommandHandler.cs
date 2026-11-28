using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Domain.Accounting.Entities;
using DomainCashShiftStatus = OAS.Domain.Accounting.Enums.CashShiftStatus;

namespace OAS.Application.Accounting.CashShifts.Commands.SetCashShiftStatus;

public sealed class SetCashShiftStatusCommandHandler(
    IRepository<CashShift, Guid> repository,
    IPermissionChecker permissionChecker,
    TimeProvider timeProvider,
    ICashShiftBalanceService balanceService)
    : IRequestHandler<SetCashShiftStatusCommand>
{
    public async Task Handle(
        SetCashShiftStatusCommand request,
        CancellationToken cancellationToken)
    {
        var shift = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (shift is null)
        {
            throw new NotFoundException(nameof(CashShift), request.Id);
        }

        if (shift.CashBoxShiftId.HasValue)
            throw new ConflictException("cash_box_shift_group_required", "استخدم إدارة الوردية المجمعة لإجراء العملية على جميع العملات.");

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!shift.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The cash shift has been modified by another user.");
        }

        var targetStatus = (DomainCashShiftStatus)(int)request.Request.Status;

        switch (targetStatus)
        {
            case DomainCashShiftStatus.Closing:
                if (shift.Status != DomainCashShiftStatus.Open)
                    throw new ConflictException("cash_shift_not_open", "Only an open shift can start closing.");
                shift.StartClosing(await balanceService.CalculateExpectedAsync(shift, timeProvider.GetUtcNow().UtcDateTime, cancellationToken));
                break;

            case DomainCashShiftStatus.Closed:
                // Status changes must never substitute expected cash for a physical count.
                throw new ConflictException("actual_closing_balance_required", "Use the close shift operation and enter the actual cash count.");

            case DomainCashShiftStatus.Approved:
                if (!await permissionChecker.HasPermissionAsync(AccountingPermissions.CashShifts.Approve, cancellationToken))
                    throw new ForbiddenException();
                shift.Approve();
                break;

            default:
                throw new InvalidOperationException($"Cash shift status '{targetStatus}' is not supported for manual transition.");
        }

        repository.Update(shift);
    }
}
