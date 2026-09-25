using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainJournalType = OAS.Domain.Accounting.Enums.JournalType;

namespace OAS.Application.Accounting.Journals.Commands.UpdateJournalEntry;

public sealed class UpdateJournalEntryCommandHandler(
    IRepository<JournalEntry, Guid> repository,
    IRepository<JournalEntryLine, Guid> lineRepository,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Supplier, Guid> suppliers)
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
        foreach (var customerId in request.Request.Lines.Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId!.Value).Distinct())
        {
            var customer = await customers.GetByIdAsync(customerId, cancellationToken);
            if (customer is null) throw new NotFoundException(nameof(Customer), customerId);
            if (!customer.IsActive) throw new ConflictException("journal_customer_inactive", "The selected customer is inactive.");
        }
        foreach (var supplierId in request.Request.Lines.Where(x => x.SupplierId.HasValue).Select(x => x.SupplierId!.Value).Distinct())
        {
            var supplier = await suppliers.GetByIdAsync(supplierId, cancellationToken);
            if (supplier is null) throw new NotFoundException(nameof(Supplier), supplierId);
            if (!supplier.IsActive) throw new ConflictException("journal_supplier_inactive", "The selected supplier is inactive.");
        }
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
