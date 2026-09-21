using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.FiscalPeriods.Commands.SetFiscalPeriodLocks;

public sealed class SetFiscalPeriodLocksCommandHandler(
    IRepository<FiscalPeriod, Guid> repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetFiscalPeriodLocksCommand, Guid>
{
    public async Task<Guid> Handle(
        SetFiscalPeriodLocksCommand request,
        CancellationToken cancellationToken)
    {
        var period =
            await repository.GetForUpdateAsync(
                request.Id,
                cancellationToken);

        if (period is null)
        {
            throw new NotFoundException(
                "fiscal_period_not_found",
                "The specified fiscal period was not found.");
        }

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!period.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The fiscal period has been modified by another user.");
        }

        if (period.Status == FiscalPeriodStatus.Closed)
        {
            throw new ConflictException(
                "fiscal_period_closed",
                "A closed fiscal period cannot have its locks changed.");
        }

        period.SetLocks(
            request.Request.SalesLocked,
            request.Request.InventoryLocked,
            request.Request.AccountingLocked);

        repository.Update(period);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return period.Id;
    }
}