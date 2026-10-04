using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalYears.Commands.SetFiscalYearStatus;

public sealed class SetFiscalYearStatusCommandHandler(
    IRepository<FiscalYear, Guid> repository,
    IAccountingReportingQueryService reports,
    IFiscalYearClosingService closingService,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<SetFiscalYearStatusCommand>
{
    public async Task Handle(
        SetFiscalYearStatusCommand request,
        CancellationToken cancellationToken)
    {
        var fiscalYear = await repository.GetForUpdateAsync(
            request.Id,
            cancellationToken);

        if (fiscalYear is null)
        {
            throw new NotFoundException(
                nameof(FiscalYear),
                request.Id);
        }

        byte[] requestedRowVersion;
        try
        {
            requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        }
        catch (FormatException ex)
        {
            throw new ConcurrencyException("بيانات التزامن الخاصة بالسنة المالية غير صالحة.", ex);
        }

        if (!fiscalYear.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The fiscal year has been modified by another user.");
        }

        switch (request.Data.Status)
        {
            case OAS.Contracts.Accounting.Enums.FiscalYearStatus.Open:
                fiscalYear.Open();
                break;

            case OAS.Contracts.Accounting.Enums.FiscalYearStatus.Closing:
                fiscalYear.StartClosing();
                break;

            case OAS.Contracts.Accounting.Enums.FiscalYearStatus.Closed:
                await CloseFiscalYearAsync(fiscalYear, cancellationToken);
                break;

            case OAS.Contracts.Accounting.Enums.FiscalYearStatus.Future:
                throw new ConflictException(
                    "fiscal_year_status_invalid",
                    "A fiscal year cannot be moved back to Future status.");

            default:
                throw new ConflictException(
                    "fiscal_year_status_invalid",
                    "The requested fiscal year status is invalid.");
        }

        repository.Update(fiscalYear);
    }

    private async Task CloseFiscalYearAsync(
        FiscalYear fiscalYear,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentUser.UserId) ||
            !Guid.TryParse(currentUser.UserId, out var userId) ||
            userId == Guid.Empty)
        {
            throw new ConflictException(
                "current_user_required",
                "A valid current user is required to close a fiscal year.");
        }

        var readiness = await reports.GetFiscalYearCloseReadinessAsync(
            fiscalYear.Id,
            cancellationToken);

        if (!readiness.CanClose)
        {
            var failed = string.Join(
                "، ",
                readiness.Checks.Where(x => !x.Passed).Select(x => x.Description));

            throw new ConflictException(
                "fiscal_year_close_not_ready",
                $"لا يمكن إقفال السنة المالية قبل معالجة متطلبات الإقفال: {failed}");
        }

        await closingService.CloseAsync(
            fiscalYear,
            userId,
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);
    }
}
