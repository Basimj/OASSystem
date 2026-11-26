using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Accounting.Abstractions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.CashShifts.Commands.CloseCashShift;

public sealed class CloseCashShiftCommandHandler(
    IRepository<CashShift, Guid> repository,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ICashShiftBalanceService balanceService)
    : IRequestHandler<CloseCashShiftCommand>
{
    public async Task Handle(
        CloseCashShiftCommand request,
        CancellationToken cancellationToken)
    {
        var shift = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (shift is null)
        {
            throw new NotFoundException(nameof(CashShift), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!shift.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The cash shift has been modified by another user.");
        }

        if (!Guid.TryParse(currentUser.UserId, out var userId))
            throw new ForbiddenException();

        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (shift.Status is not (CashShiftStatus.Open or CashShiftStatus.Closing))
            throw new ConflictException("cash_shift_not_open", "Only an open or closing shift can be closed.");
        if (request.Request.ActualClosingBalance is not decimal actual || actual < 0)
            throw new ConflictException("actual_closing_balance_required", "Enter the physical cash count explicitly (zero is allowed).");

        // Recompute even for an already-closing shift; cash movements may have been posted since StartClosing.
        var expected = await balanceService.CalculateExpectedAsync(shift, now, cancellationToken);
        if (shift.Status == CashShiftStatus.Open)
            shift.StartClosing(expected);
        else
            shift.RefreshExpectedClosingBalance(expected);

        shift.Close(actual, userId, now);
        repository.Update(shift);
    }
}
