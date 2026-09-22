using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainJournalEntryStatus = OAS.Domain.Accounting.Enums.JournalEntryStatus;
using DomainJournalType = OAS.Domain.Accounting.Enums.JournalType;

namespace OAS.Application.Accounting.Journals.Commands.ReverseJournalEntry;

public sealed class ReverseJournalEntryCommandHandler(
    IRepository<JournalEntry, Guid> repository,
    IReadRepository<JournalEntryLine, Guid> lineRepository,
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<ReverseJournalEntryCommand, Guid>
{
    public async Task<Guid> Handle(
        ReverseJournalEntryCommand request,
        CancellationToken cancellationToken)
    {
        var original = await repository.GetForUpdateAsync(request.JournalEntryId, cancellationToken);
        if (original is null)
            throw new NotFoundException("journal_entry_not_found", request.JournalEntryId);

        if (original.Status != DomainJournalEntryStatus.Posted)
            throw new InvalidOperationException("Only posted journal entries can be reversed.");

        var originalLines = await lineRepository.ListAsync(
            new Specification<JournalEntryLine>()
                .Where(x => x.JournalEntryId == original.Id)
                .AddSort(nameof(JournalEntryLine.LineNumber), OAS.Contracts.Common.Pagination.SortDirection.Ascending),
            cancellationToken);

        if (originalLines.Count == 0)
        {
            throw new ConflictException(
                "journal_lines_missing",
                "The posted journal has no lines and cannot be reversed safely.");
        }

        var sequence = await sequenceNumberGenerator.NextAsync(
            $"JournalEntry-{original.PostingDate.Year}", cancellationToken);
        var journalNumber = $"JV-{original.PostingDate.Year:0000}-{sequence:000000}";
        var reversalId = Guid.NewGuid();

        var reversal = JournalEntry.Create(
            reversalId,
            journalNumber,
            DomainJournalType.Reversal,
            original.PostingDate,
            original.DocumentDate,
            original.FiscalPeriodId,
            $"Reversal of {original.JournalNumber}",
            "Accounting",
            "JournalEntryReversal",
            original.Id,
            DomainJournalEntryStatus.Draft);

        foreach (var originalLine in originalLines)
        {
            reversal.AddLine(JournalEntryLine.Create(
                Guid.NewGuid(),
                reversalId,
                originalLine.LineNumber,
                originalLine.AccountId,
                originalLine.CreditAmount,
                originalLine.DebitAmount,
                originalLine.Description,
                originalLine.CustomerId,
                originalLine.SupplierId,
                originalLine.CostCenterId,
                originalLine.ProductVariantId,
                originalLine.WarehouseId));
        }

        original.MarkReversed(reversalId);
        await repository.AddAsync(reversal, cancellationToken);
        repository.Update(original);
        return reversalId;
    }
}
