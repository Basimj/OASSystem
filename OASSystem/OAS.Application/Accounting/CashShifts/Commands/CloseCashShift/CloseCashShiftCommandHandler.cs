using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.CashShifts.Commands.CloseCashShift;

public sealed class CloseCashShiftCommandHandler(
    IRepository<CashShift, Guid> repository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
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

        if (shift.Status == CashShiftStatus.Open)
        {
            shift.StartClosing(shift.ExpectedClosingBalance ?? shift.OpeningBalance);
        }

        shift.Close(request.Request.ActualClosingBalance, userId, now);
        repository.Update(shift);
    }
}
