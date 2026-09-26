using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainJournalEntryStatus = OAS.Domain.Accounting.Enums.JournalEntryStatus;
using DomainJournalType = OAS.Domain.Accounting.Enums.JournalType;
namespace OAS.Application.Accounting.Journals.Commands.ReverseJournalEntry;
public sealed class ReverseJournalEntryCommandHandler(IRepository<JournalEntry, Guid> repository,IReadRepository<JournalEntryLine, Guid> lineRepository,ISequenceNumberGenerator sequenceNumberGenerator) : IRequestHandler<ReverseJournalEntryCommand, Guid>
{
    public async Task<Guid> Handle(ReverseJournalEntryCommand request, CancellationToken ct)
    {
        var original = await repository.GetForUpdateAsync(request.JournalEntryId, ct) ?? throw new NotFoundException(nameof(JournalEntry), request.JournalEntryId);
        if (original.Status != DomainJournalEntryStatus.Posted) throw new ConflictException("journal_not_posted", "Only posted journal entries can be reversed.");
        var originalLines = await lineRepository.ListAsync(new Specification<JournalEntryLine>().Where(x => x.JournalEntryId == original.Id).AddSort(nameof(JournalEntryLine.LineNumber), OAS.Contracts.Common.Pagination.SortDirection.Ascending), ct);
        if (originalLines.Count == 0) throw new ConflictException("journal_lines_missing", "The posted journal has no lines and cannot be reversed safely.");
        var sequence = await sequenceNumberGenerator.NextAsync($"JournalEntry-{original.PostingDate.Year}", ct);
        var reversalId = Guid.NewGuid();
        var reversal = JournalEntry.Create(reversalId,$"JV-{original.PostingDate.Year:0000}-{sequence:000000}",DomainJournalType.Reversal,original.PostingDate,original.DocumentDate,original.FiscalPeriodId,$"Reversal of {original.JournalNumber}","Accounting","JournalEntryReversal",original.Id,DomainJournalEntryStatus.Draft);
        if (original.BaseCurrencyId is Guid baseId && !string.IsNullOrWhiteSpace(original.BaseCurrencyCodeSnapshot) && original.BaseCurrencyDecimalPlacesSnapshot is byte baseDp)
            reversal.SetBaseCurrencySnapshot(baseId, original.BaseCurrencyCodeSnapshot, baseDp);

        foreach (var line in originalLines)
        {
            JournalEntryLine reversed;
            if (line.TransactionCurrencyId is Guid currencyId && !string.IsNullOrWhiteSpace(line.TransactionCurrencyCodeSnapshot) && line.TransactionCurrencyDecimalPlacesSnapshot is byte dp && line.TransactionDebitAmount.HasValue && line.TransactionCreditAmount.HasValue && line.ExchangeRate.HasValue && line.ExchangeRateDate.HasValue && line.ExchangeRateType.HasValue && line.ExchangeRateSource.HasValue)
            {
                reversed = JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(),reversalId,line.LineNumber,line.AccountId,line.CreditAmount,line.DebitAmount,currencyId,line.TransactionCurrencyCodeSnapshot,dp,line.TransactionCreditAmount.Value,line.TransactionDebitAmount.Value,line.ExchangeRate.Value,line.ExchangeRateDate.Value,line.ExchangeRateType.Value,line.ExchangeRateSource.Value,line.Description,line.CustomerId,line.SupplierId,line.EmployeeId,line.PartyNameSnapshot,line.CostCenterId,line.ProductVariantId,line.WarehouseId,line.SourceDocumentLineId);
            }
            else
            {
                reversed = JournalEntryLine.Create(Guid.NewGuid(),reversalId,line.LineNumber,line.AccountId,line.CreditAmount,line.DebitAmount,line.Description,line.CustomerId,line.SupplierId,line.CostCenterId,line.ProductVariantId,line.WarehouseId);
            }
            reversal.AddLine(reversed);
        }
        original.MarkReversed(reversalId);
        await repository.AddAsync(reversal, ct);
        repository.Update(original);
        return reversalId;
    }
}
