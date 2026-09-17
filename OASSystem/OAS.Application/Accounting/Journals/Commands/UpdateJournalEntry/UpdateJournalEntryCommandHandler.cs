using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainJournalType = OAS.Domain.Accounting.Enums.JournalType;

namespace OAS.Application.Accounting.Journals.Commands.UpdateJournalEntry;

public sealed class UpdateJournalEntryCommandHandler(
    IRepository<JournalEntry, Guid> repository)
    : IRequestHandler<UpdateJournalEntryCommand, Guid>
{
    public async Task<Guid> Handle(
        UpdateJournalEntryCommand request,
        CancellationToken cancellationToken)
    {
        var journal =
            await repository.GetForUpdateAsync(
                request.JournalEntryId,
                cancellationToken);

        if (journal is null)
        {
            throw new NotFoundException(
                "journal_entry_not_found",
                request.JournalEntryId);
        }

        var requestedRowVersion =
            Convert.FromBase64String(
                request.Request.RowVersion);

        if (!journal.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException(
                "The journal entry has been modified by another user.");
        }

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

        journal.ClearLines();

        var lineNumber = 1;

        foreach (var lineRequest in request.Request.Lines)
        {
            var line =
                JournalEntryLine.Create(
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
                    lineRequest.WarehouseId);

            journal.AddLine(line);
        }

        repository.Update(journal);

        return journal.Id;
    }
}