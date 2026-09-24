using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Contracts.Accounting.Journals;
using OAS.Domain.Accounting.Entities;
using DomainJournalEntryStatus = OAS.Domain.Accounting.Enums.JournalEntryStatus;
using DomainJournalType = OAS.Domain.Accounting.Enums.JournalType;

namespace OAS.Application.Accounting.Journals.Commands.CreateJournalEntry;

public sealed class CreateJournalEntryCommandHandler(
    IRepository<JournalEntry, Guid> repository,
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<CreateJournalEntryCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateJournalEntryCommand request,
        CancellationToken cancellationToken)
    {
        var data = request.Request;

        var sequenceName =
            $"JournalEntry-{data.PostingDate.Year}";

        var sequence =
            await sequenceNumberGenerator.NextAsync(
                sequenceName,
                cancellationToken);

        var journalNumber =
            $"JV-{data.PostingDate.Year:0000}-{sequence:000000}";

        var journalId = Guid.NewGuid();

        var journal =
            JournalEntry.Create(
                journalId,
                journalNumber,
                (DomainJournalType)(int)data.JournalType,
                data.PostingDate,
                data.DocumentDate,
                data.FiscalPeriodId,
                data.Description,
                data.SourceModule,
                data.SourceDocumentType,
                data.SourceDocumentId,
                DomainJournalEntryStatus.Draft);

        var lineNumber = 1;

        foreach (var lineRequest in data.Lines)
        {
            var line =
                JournalEntryLine.Create(
                    Guid.NewGuid(),
                    journalId,
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

        await repository.AddAsync(
            journal,
            cancellationToken);

        return journal.Id;
    }
}
