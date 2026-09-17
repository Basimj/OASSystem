using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Domain.Accounting.Entities;
using DomainCashShiftStatus = OAS.Domain.Accounting.Enums.CashShiftStatus;

namespace OAS.Application.Accounting.CashShifts.Commands.SetCashShiftStatus;

public sealed class SetCashShiftStatusCommandHandler(
    IRepository<CashShift, Guid> repository,
    IPermissionChecker permissionChecker,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
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

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!shift.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The cash shift has been modified by another user.");
        }

        var targetStatus = (DomainCashShiftStatus)(int)request.Request.Status;

        switch (targetStatus)
        {
            case DomainCashShiftStatus.Closing:
                shift.StartClosing(shift.ExpectedClosingBalance ?? shift.OpeningBalance);
                break;

            case DomainCashShiftStatus.Closed:
                if (!Guid.TryParse(currentUser.UserId, out var closedBy))
                    throw new ForbiddenException();
                if (shift.Status == DomainCashShiftStatus.Open)
                {
                    shift.StartClosing(shift.ExpectedClosingBalance ?? shift.OpeningBalance);
                }
                shift.Close(shift.ExpectedClosingBalance ?? shift.OpeningBalance, closedBy, timeProvider.GetUtcNow().UtcDateTime);
                break;

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
