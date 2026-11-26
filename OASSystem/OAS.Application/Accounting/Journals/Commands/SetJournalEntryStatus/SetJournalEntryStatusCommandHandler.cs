using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.CoreHardening.Services;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Application.Accounting.Journals.Mapping;
using DomainJournalType = OAS.Domain.Accounting.Enums.JournalType;
using DomainJournalEntryStatus = OAS.Domain.Accounting.Enums.JournalEntryStatus;

namespace OAS.Application.Accounting.Journals.Commands.SetJournalEntryStatus;

public sealed class SetJournalEntryStatusCommandHandler(
    IRepository<JournalEntry, Guid> repository,
    IReadRepository<JournalEntryLine, Guid> lineRepository,
    IReadRepository<FiscalPeriod, Guid> periodRepository,
    IReadRepository<Account, Guid> accountRepository,
    IJournalSubledgerIntegrityService subledgerIntegrity,
    IJournalSourceIntegrityService sourceIntegrity,
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

        // Both repositories share the same scoped EF Core DbContext. The tracked
        // query hydrates journal.Lines through relationship fix-up automatically.
        // Do not call AddLine here: it would re-add existing lines and throw.
        await lineRepository.ListAsync(
            new Specification<JournalEntryLine>()
                .Where(x => x.JournalEntryId == journal.Id)
                .AddSort(
                    nameof(JournalEntryLine.LineNumber),
                    OAS.Contracts.Common.Pagination.SortDirection.Ascending)
                .Tracking(),
            cancellationToken);

        var status = (DomainJournalEntryStatus)(int)request.Request.Status;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (status is DomainJournalEntryStatus.PendingApproval or DomainJournalEntryStatus.Approved or DomainJournalEntryStatus.Posted)
            await ValidateReversalAsync(journal, cancellationToken);

        switch (status)
        {
            case DomainJournalEntryStatus.PendingApproval:
                journal.SetPendingApproval();
                break;

            case DomainJournalEntryStatus.Approved:
                journal.Approve(userId, nowUtc);
                break;

            case DomainJournalEntryStatus.Posted:
                await sourceIntegrity.ValidateUniqueSourceAsync(
                    journal.SourceModule,
                    journal.SourceDocumentType,
                    journal.SourceDocumentId,
                    journal.Id,
                    cancellationToken);

                var period = await periodRepository.GetByIdAsync(journal.FiscalPeriodId, cancellationToken);
                if (period is null)
                    throw new NotFoundException(nameof(FiscalPeriod), journal.FiscalPeriodId);

                if (!period.CanPostAccounting() || journal.PostingDate < period.StartDate || journal.PostingDate > period.EndDate)
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

                    await subledgerIntegrity.ValidateAndResolvePartyNameAsync(
                        account,
                        line.CustomerId,
                        line.SupplierId,
                        line.EmployeeId,
                        cancellationToken);
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

    private async Task ValidateReversalAsync(JournalEntry journal, CancellationToken ct)
    {
        var originals = await repository.ListAsync(new Specification<JournalEntry>().Where(x => x.ReversedJournalId == journal.Id), ct);
        if (originals.Count == 0 && !JournalReversalInfoResolver.IsKnownReversal(journal)) return;
        if (originals.Count != 1 || journal.JournalType != DomainJournalType.Reversal || journal.SourceDocumentType != "JournalEntryReversal" || journal.SourceDocumentId != originals[0].Id)
            throw new ConflictException("journal_reversal_link_invalid", "القيد العكسي غير مرتبط بقيد أصلي صالح، أو يتبع مستندًا تشغيليًا. راجع مصدره قبل الترحيل.");
        var original = originals[0];
        if (original.Status != DomainJournalEntryStatus.Posted || original.JournalType != DomainJournalType.Manual || original.SourceDocumentId.HasValue || !string.IsNullOrWhiteSpace(original.SourceDocumentType) || (!string.IsNullOrWhiteSpace(original.SourceModule) && !string.Equals(original.SourceModule, "Accounting", StringComparison.OrdinalIgnoreCase)) ||
            await repository.CountAsync(new Specification<JournalEntry>().Where(x => x.ReversedJournalId == original.Id), ct) > 0 || journal.PostingDate < original.PostingDate)
            throw new ConflictException("journal_reverse_source_required", "لا يمكن ترحيل عكس مباشر لقيد غير يدوي مستقل أو لقيد عكسي آخر.");
        var originalLines = await lineRepository.ListAsync(new Specification<JournalEntryLine>().Where(x => x.JournalEntryId == original.Id), ct);
        var expected = originalLines.OrderBy(x => x.LineNumber).ToArray();
        var actual = journal.Lines.OrderBy(x => x.LineNumber).ToArray();
        if (expected.Length == 0 || expected.Length != actual.Length || journal.BaseCurrencyId != original.BaseCurrencyId || journal.BaseCurrencyCodeSnapshot != original.BaseCurrencyCodeSnapshot || journal.BaseCurrencyDecimalPlacesSnapshot != original.BaseCurrencyDecimalPlacesSnapshot ||
            !actual.Zip(expected).All(pair => pair.First.IsExactOppositeOf(pair.Second)))
            throw new ConflictException("journal_reversal_amount_mismatch", "بيانات القيد العكسي لا تطابق القيد الأصلي. لا يمكن اعتماده أو ترحيله قبل معالجة القيد بواسطة المسؤول.");
    }
}
