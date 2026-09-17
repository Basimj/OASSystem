using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashShifts.Commands.ApproveCashShift;

public sealed class ApproveCashShiftCommandHandler(
    IRepository<CashShift, Guid> repository)
    : IRequestHandler<ApproveCashShiftCommand>
{
    public async Task Handle(
        ApproveCashShiftCommand request,
        CancellationToken cancellationToken)
    {
        var shift = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (shift is null)
        {
            throw new NotFoundException(nameof(CashShift), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.RowVersion);
        if (!shift.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The cash shift has been modified by another user.");
        }

        shift.Approve();
        repository.Update(shift);
    }
}
