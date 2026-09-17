using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Domain.Accounting.Entities;
using DomainCashShiftStatus = OAS.Domain.Accounting.Enums.CashShiftStatus;

namespace OAS.Application.Accounting.CashShifts.Commands.CreateCashShift;

public sealed class CreateCashShiftCommandHandler(
    IRepository<CashShift, Guid> repository,
    ISequenceNumberGenerator sequenceNumberGenerator,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<CreateCashShiftCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateCashShiftCommand request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId))
            throw new ForbiddenException();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var sequenceName = $"CashShift-{now.Year}";
        var sequence = await sequenceNumberGenerator.NextAsync(sequenceName, cancellationToken);
        var shiftNumber = $"CS-{now.Year:0000}-{sequence:000000}";

        var shift = CashShift.Create(
            Guid.NewGuid(),
            shiftNumber,
            request.Data.CashAccountId,
            userId,
            now,
            request.Data.OpeningBalance,
            DomainCashShiftStatus.Open);

        await repository.AddAsync(shift, cancellationToken);
        return shift.Id;
    }
}
