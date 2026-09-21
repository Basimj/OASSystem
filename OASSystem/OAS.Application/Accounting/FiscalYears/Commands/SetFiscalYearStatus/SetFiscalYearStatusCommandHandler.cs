using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalYears.Commands.SetFiscalYearStatus;

public sealed class SetFiscalYearStatusCommandHandler(
    IRepository<FiscalYear, Guid> repository,
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

        switch (request.Data.Status)
        {
            case OAS.Contracts.Accounting.Enums.FiscalYearStatus.Open:
                fiscalYear.Open();
                break;

            case OAS.Contracts.Accounting.Enums.FiscalYearStatus.Closing:
                fiscalYear.StartClosing();
                break;

            case OAS.Contracts.Accounting.Enums.FiscalYearStatus.Closed:
                CloseFiscalYear(fiscalYear);
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

    private void CloseFiscalYear(FiscalYear fiscalYear)
    {
        if (string.IsNullOrWhiteSpace(currentUser.UserId) ||
            !Guid.TryParse(currentUser.UserId, out var userId) ||
            userId == Guid.Empty)
        {
            throw new ConflictException(
                "current_user_required",
                "A valid current user is required to close a fiscal year.");
        }

        fiscalYear.Close(
            userId,
            timeProvider.GetUtcNow().UtcDateTime);
    }
}