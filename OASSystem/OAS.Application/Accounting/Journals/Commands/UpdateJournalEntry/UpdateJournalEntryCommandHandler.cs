using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainJournalType = OAS.Domain.Accounting.Enums.JournalType;

namespace OAS.Application.Accounting.Journals.Commands.UpdateJournalEntry;

public sealed class UpdateJournalEntryCommandHandler(
    IRepository<JournalEntry, Guid> repository,
    IRepository<JournalEntryLine, Guid> lineRepository)
    : IRequestHandler<UpdateJournalEntryCommand, Guid>
{
    public async Task<Guid> Handle(
        UpdateJournalEntryCommand request,
        CancellationToken cancellationToken)
    {
        var journal = await repository.GetForUpdateAsync(request.JournalEntryId, cancellationToken);
        if (journal is null)
            throw new NotFoundException("journal_entry_not_found", request.JournalEntryId);

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!journal.RowVersion.SequenceEqual(requestedRowVersion))
            throw new ConcurrencyException("The journal entry has been modified by another user.");

        journal.EnsureEditable();
        journal.UpdateDraft(
            (DomainJournalType)(int)request.Request.JournalType,
            request.Request.PostingDate,
            request.Request.DocumentDate,
            request.Request.FiscalPeriodId,
            request.Request.Description,
            request.Request.SourceModule,
            request.Request.SourceDocumentType,
            request.Request.SourceDocumentId);

        var existingLines = await lineRepository.ListAsync(
            new Specification<JournalEntryLine>()
                .Where(x => x.JournalEntryId == journal.Id)
                .Tracking(),
            cancellationToken);

        if (existingLines.Count > 0)
            lineRepository.DeleteRange(existingLines);

        var lineNumber = 1;
        var newLines = request.Request.Lines
            .Select(lineRequest => JournalEntryLine.Create(
                Guid.NewGuid(),
                journal.Id,
                lineNumber++,
                lineRequest.AccountId,
                lineRequest.DebitAmount,
                lineRequest.CreditAmount,
                lineRequest.Description,
                lineRequest.CustomerId,
                lineRequest.SupplierId,
                lineRequest.CostCenterId,
                lineRequest.ProductVariantId,
                lineRequest.WarehouseId))
            .ToList();

        journal.ReplaceLines(newLines);
        if (newLines.Count > 0)
            await lineRepository.AddRangeAsync(newLines, cancellationToken);

        repository.Update(journal);
        return journal.Id;
    }
}
