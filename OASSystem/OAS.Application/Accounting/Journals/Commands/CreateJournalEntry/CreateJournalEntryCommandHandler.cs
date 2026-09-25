using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainJournalEntryStatus = OAS.Domain.Accounting.Enums.JournalEntryStatus;
using DomainJournalType = OAS.Domain.Accounting.Enums.JournalType;

namespace OAS.Application.Accounting.Journals.Commands.CreateJournalEntry;

public sealed class CreateJournalEntryCommandHandler(
    IRepository<JournalEntry, Guid> repository,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Supplier, Guid> suppliers,
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<CreateJournalEntryCommand, Guid>
{
    public async Task<Guid> Handle(CreateJournalEntryCommand request, CancellationToken cancellationToken)
    {
        var data = request.Request;
        var journalNumber = await ResolveNumberAsync(data.JournalNumber, data.PostingDate.Year, cancellationToken);
        await ValidatePartiesAsync(data.Lines, cancellationToken);

        var journalId = Guid.NewGuid();
        var journal = JournalEntry.Create(
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
            journal.AddLine(JournalEntryLine.Create(
                Guid.NewGuid(), journalId, lineNumber++, lineRequest.AccountId,
                lineRequest.DebitAmount, lineRequest.CreditAmount, lineRequest.Description,
                lineRequest.CustomerId, lineRequest.SupplierId, lineRequest.CostCenterId,
                lineRequest.ProductVariantId, lineRequest.WarehouseId));
        }

        await repository.AddAsync(journal, cancellationToken);
        return journal.Id;
    }

    private async Task<string> ResolveNumberAsync(string? requested, int year, CancellationToken ct)
    {
        var number = requested?.Trim();
        if (!string.IsNullOrEmpty(number) && !number.StartsWith($"JV-{year:0000}-", StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("journal_number_period_mismatch", "Reserved journal number does not match the posting year.");

        if (string.IsNullOrEmpty(number))
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var sequence = await sequenceNumberGenerator.NextAsync($"JournalEntry-{year}", ct);
                number = $"JV-{year:0000}-{sequence:000000}";
                if (!await NumberExistsAsync(number, ct)) break;
            }
        }

        if (string.IsNullOrEmpty(number) || await NumberExistsAsync(number, ct))
            throw new ConflictException("journal_number_duplicate", "Journal number is already in use.");
        return number;
    }

    private Task<long> NumberExistsCountAsync(string number, CancellationToken ct) =>
        repository.CountAsync(new Specification<JournalEntry>().Where(x => x.JournalNumber == number), ct);
    private async Task<bool> NumberExistsAsync(string number, CancellationToken ct) =>
        await NumberExistsCountAsync(number, ct) > 0;

    private async Task ValidatePartiesAsync(IEnumerable<OAS.Contracts.Accounting.Journals.CreateJournalEntryLineRequest> lines, CancellationToken ct)
    {
        foreach (var customerId in lines.Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId!.Value).Distinct())
        {
            var customer = await customers.GetByIdAsync(customerId, ct);
            if (customer is null) throw new NotFoundException(nameof(Customer), customerId);
            if (!customer.IsActive) throw new ConflictException("journal_customer_inactive", "The selected customer is inactive.");
        }
        foreach (var supplierId in lines.Where(x => x.SupplierId.HasValue).Select(x => x.SupplierId!.Value).Distinct())
        {
            var supplier = await suppliers.GetByIdAsync(supplierId, ct);
            if (supplier is null) throw new NotFoundException(nameof(Supplier), supplierId);
            if (!supplier.IsActive) throw new ConflictException("journal_supplier_inactive", "The selected supplier is inactive.");
        }
    }
}
