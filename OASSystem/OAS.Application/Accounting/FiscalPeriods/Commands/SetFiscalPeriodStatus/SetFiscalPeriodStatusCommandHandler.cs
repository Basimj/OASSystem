using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Enums;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.FiscalPeriods.Commands.SetFiscalPeriodStatus;

public sealed class SetFiscalPeriodStatusCommandHandler(
    IRepository<FiscalPeriod, Guid> repository,
    IUnitOfWork unitOfWork,
    IAccountingReportingQueryService reports,
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

        byte[] requestedRowVersion;
        try
        {
            requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        }
        catch (FormatException ex)
        {
            throw new ConcurrencyException("بيانات التزامن الخاصة بالفترة المالية غير صالحة.", ex);
        }

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

                var readiness = await reports.GetFiscalPeriodCloseReadinessAsync(
                    period.Id,
                    cancellationToken);

                if (!readiness.CanClose)
                {
                    var failed = string.Join(
                        "، ",
                        readiness.Checks.Where(x => !x.Passed).Select(x => x.Description));

                    throw new ConflictException(
                        "fiscal_period_close_not_ready",
                        $"لا يمكن إقفال الفترة المالية قبل معالجة متطلبات الإقفال: {failed}");
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