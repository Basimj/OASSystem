using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainJournalEntryStatus = OAS.Domain.Accounting.Enums.JournalEntryStatus;
using DomainJournalType = OAS.Domain.Accounting.Enums.JournalType;
namespace OAS.Application.Accounting.Journals.Commands.ReverseJournalEntry;
public sealed class ReverseJournalEntryCommandHandler(IRepository<JournalEntry, Guid> repository,IReadRepository<JournalEntryLine, Guid> lineRepository,ISequenceNumberGenerator sequenceNumberGenerator, IReadRepository<FiscalPeriod, Guid> periodRepository) : IRequestHandler<ReverseJournalEntryCommand, Guid>
{
    public async Task<Guid> Handle(ReverseJournalEntryCommand request, CancellationToken ct)
    {
        var original = await repository.GetForUpdateAsync(request.JournalEntryId, ct) ?? throw new NotFoundException(nameof(JournalEntry), request.JournalEntryId);
        if (original.Status != DomainJournalEntryStatus.Posted)
            throw new ConflictException("journal_not_posted", "Only posted journal entries can be reversed.");
        if (original.ReversedJournalId.HasValue)
            throw new ConflictException("journal_already_reversed", "This journal entry has already been reversed.");
        if (original.JournalType != DomainJournalType.Manual || original.SourceDocumentId.HasValue || !string.IsNullOrWhiteSpace(original.SourceDocumentType) || (!string.IsNullOrWhiteSpace(original.SourceModule) && !string.Equals(original.SourceModule, "Accounting", StringComparison.OrdinalIgnoreCase)))
            throw new ConflictException("journal_reverse_source_required", "لا يمكن عكس هذا القيد مباشرة. استخدم إجراء التصحيح في المستند المصدر أو الإجراء المحاسبي المختص.");
        if (await repository.CountAsync(new Specification<JournalEntry>().Where(j => j.ReversedJournalId == original.Id), ct) > 0)
            throw new ConflictException("journal_reversal_not_reversible", "لا يمكن عكس قيد عكسي من شاشة القيود اليومية.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 400)
            throw new ConflictException("journal_reverse_reason_required", "يجب إدخال سبب العكس بما لا يتجاوز 400 حرف.");
        if (request.PostingDate < original.PostingDate)
            throw new ConflictException("journal_reverse_date_invalid", "لا يجوز أن يسبق تاريخ العكس تاريخ ترحيل القيد الأصلي.");
        var periods = await periodRepository.ListAsync(new Specification<FiscalPeriod>().Where(x => x.StartDate <= request.PostingDate && x.EndDate >= request.PostingDate), ct);
        if (periods.Count != 1 || !periods[0].CanPostAccounting())
            throw new ConflictException("journal_reverse_period_invalid", "يجب اختيار تاريخ يقع ضمن فترة مالية واحدة مفتوحة للترحيل.");
        var originalLines = await lineRepository.ListAsync(new Specification<JournalEntryLine>().Where(x => x.JournalEntryId == original.Id).AddSort(nameof(JournalEntryLine.LineNumber), OAS.Contracts.Common.Pagination.SortDirection.Ascending), ct);
        if (originalLines.Count == 0) throw new ConflictException("journal_lines_missing", "The posted journal has no lines and cannot be reversed safely.");
        OAS.Domain.Accounting.Rules.JournalPostingRules.EnsureBalanced(originalLines);
        OAS.Domain.Accounting.Rules.JournalPostingRules.EnsureCurrencySnapshots(original, originalLines);
        var sequence = await sequenceNumberGenerator.NextAsync($"JournalEntry-{request.PostingDate.Year}", ct);
        var reversalId = Guid.NewGuid();
        var reversal = JournalEntry.Create(reversalId,$"JV-{request.PostingDate.Year:0000}-{sequence:000000}",DomainJournalType.Reversal,request.PostingDate,request.PostingDate,periods[0].Id,$"عكس القيد {original.JournalNumber}: {request.Reason.Trim()}","Accounting","JournalEntryReversal",original.Id,DomainJournalEntryStatus.Draft);
        if (original.BaseCurrencyId is Guid baseId && !string.IsNullOrWhiteSpace(original.BaseCurrencyCodeSnapshot) && original.BaseCurrencyDecimalPlacesSnapshot is byte baseDp)
            reversal.SetBaseCurrencySnapshot(baseId, original.BaseCurrencyCodeSnapshot, baseDp);

        foreach (var line in originalLines)
            reversal.AddLine(line.CreateOpposite(Guid.NewGuid(), reversalId));
        original.MarkReversed(reversalId);
        await repository.AddAsync(reversal, ct);
        repository.Update(original);
        return reversalId;
    }
}
