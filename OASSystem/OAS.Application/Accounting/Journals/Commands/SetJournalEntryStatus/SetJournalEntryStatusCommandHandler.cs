using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainJournalEntryStatus = OAS.Domain.Accounting.Enums.JournalEntryStatus;

namespace OAS.Application.Accounting.Journals.Commands.SetJournalEntryStatus;

public sealed class SetJournalEntryStatusCommandHandler(
    IRepository<JournalEntry, Guid> repository,
    IReadRepository<JournalEntryLine, Guid> lineRepository,
    IReadRepository<FiscalPeriod, Guid> periodRepository,
    IReadRepository<Account, Guid> accountRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<SetJournalEntryStatusCommand, Guid>
{
    public async Task<Guid> Handle(
        SetJournalEntryStatusCommand request,
        CancellationToken cancellationToken)
    {
        var journal = await repository.GetForUpdateAsync(request.JournalEntryId, cancellationToken);
        if (journal is null)
            throw new NotFoundException("journal_entry_not_found", request.JournalEntryId);

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!journal.RowVersion.SequenceEqual(requestedRowVersion))
            throw new ConcurrencyException("The journal entry has been modified by another user.");

        if (!Guid.TryParse(currentUser.UserId, out var userId))
            throw new ForbiddenException();

        // The generic aggregate repository does not eager-load child rows. Hydrate the
        // domain aggregate explicitly so balance/state rules operate on the real journal.
        var lines = await lineRepository.ListAsync(
            new Specification<JournalEntryLine>()
                .Where(x => x.JournalEntryId == journal.Id)
                .AddSort(nameof(JournalEntryLine.LineNumber), OAS.Contracts.Common.Pagination.SortDirection.Ascending)
                .Tracking(),
            cancellationToken);

        foreach (var line in lines)
            journal.AddLine(line);

        var status = (DomainJournalEntryStatus)(int)request.Request.Status;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        switch (status)
        {
            case DomainJournalEntryStatus.PendingApproval:
                journal.SetPendingApproval();
                break;

            case DomainJournalEntryStatus.Approved:
                journal.Approve(userId, nowUtc);
                break;

            case DomainJournalEntryStatus.Posted:
                var period = await periodRepository.GetByIdAsync(journal.FiscalPeriodId, cancellationToken);
                if (period is null)
                    throw new NotFoundException(nameof(FiscalPeriod), journal.FiscalPeriodId);

                if (!period.CanPostAccounting())
                {
                    throw new ConflictException(
                        "fiscal_period_closed",
                        "Cannot post journal entry into a closed or accounting-locked fiscal period.");
                }

                var isManual = journal.JournalType == OAS.Domain.Accounting.Enums.JournalType.Manual;
                foreach (var line in journal.Lines)
                {
                    var account = await accountRepository.GetByIdAsync(line.AccountId, cancellationToken);
                    if (account is null)
                        throw new NotFoundException(nameof(Account), line.AccountId);

                    if (!account.IsActive)
                    {
                        throw new ConflictException(
                            "account_inactive",
                            $"Cannot post to inactive account '{account.Code}'.");
                    }

                    if (account.AccountType == OAS.Domain.Accounting.Enums.AccountType.Header || !account.IsPostingAccount)
                    {
                        throw new ConflictException(
                            "header_account_posting_not_allowed",
                            $"Cannot post to header or non-posting account '{account.Code}'.");
                    }

                    if (isManual && !account.AllowManualPosting)
                    {
                        throw new ConflictException(
                            "manual_posting_not_allowed",
                            $"Manual posting is not allowed for account '{account.Code}'.");
                    }
                }

                journal.Post(userId, nowUtc);
                break;

            default:
                throw new InvalidOperationException(
                    $"Journal status '{status}' cannot be set through this operation.");
        }

        repository.Update(journal);
        return journal.Id;
    }
}
