using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Enums;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.FiscalPeriods.Commands.SetFiscalPeriodStatus;

public sealed class SetFiscalPeriodStatusCommandHandler(
    IRepository<FiscalPeriod, Guid> repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<SetFiscalPeriodStatusCommand, Guid>
{
    public async Task<Guid> Handle(
        SetFiscalPeriodStatusCommand request,
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

        switch (request.Request.Status)
        {
            case Contracts.Accounting.Enums.FiscalPeriodStatus.Open:
                throw new ConflictException(
                    "fiscal_period_reopen_not_supported",
                    "A closed or soft-closed fiscal period cannot be reopened.");

            case Contracts.Accounting.Enums.FiscalPeriodStatus.SoftClosed:
                period.SoftClose();
                break;

            case Contracts.Accounting.Enums.FiscalPeriodStatus.Closed:
                if (string.IsNullOrWhiteSpace(currentUser.UserId) ||
                    !Guid.TryParse(currentUser.UserId, out var userId) ||
                    userId == Guid.Empty)
                {
                    throw new ConflictException(
                        "current_user_required",
                        "A valid current user is required to close a fiscal period.");
                }

                period.Close(
                    userId,
                    timeProvider.GetUtcNow().UtcDateTime);

                break;

            default:
                throw new ConflictException(
                    "invalid_fiscal_period_status",
                    "The requested fiscal period status is not supported.");
        }

        repository.Update(period);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return period.Id;
    }
}