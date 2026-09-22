using OAS.Contracts.Accounting.Journals;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Journals.Mapping;

public static class JournalEntryMapping
{
    public static JournalEntryDto ToDto(JournalEntry journal)
        => ToDto(journal, journal.Lines);

    public static JournalEntryDto ToDto(
        JournalEntry journal,
        IEnumerable<JournalEntryLine> lines)
    {
        return new JournalEntryDto(
            journal.Id,
            journal.JournalNumber,
            (OAS.Contracts.Accounting.Enums.JournalType)(int)journal.JournalType,
            journal.PostingDate,
            journal.DocumentDate,
            journal.FiscalPeriodId,
            journal.Description,
            journal.SourceModule,
            journal.SourceDocumentType,
            journal.SourceDocumentId,
            (OAS.Contracts.Accounting.Enums.JournalEntryStatus)(int)journal.Status,
            journal.ReversedJournalId,
            journal.CreatedBy,
            journal.CreatedAtUtc,
            journal.ApprovedBy,
            journal.ApprovedAtUtc.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(journal.ApprovedAtUtc.Value, DateTimeKind.Utc))
                : null,
            journal.PostedBy,
            journal.PostedAtUtc.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(journal.PostedAtUtc.Value, DateTimeKind.Utc))
                : null,
            Convert.ToBase64String(journal.RowVersion),
            lines.OrderBy(x => x.LineNumber).Select(ToLineDto).ToArray());
    }

    public static JournalEntryLineDto ToLineDto(JournalEntryLine line)
    {
        return new JournalEntryLineDto(
            line.Id,
            line.JournalEntryId,
            line.LineNumber,
            line.AccountId,
            line.DebitAmount,
            line.CreditAmount,
            line.Description,
            line.CustomerId,
            line.SupplierId,
            line.CostCenterId,
            line.ProductVariantId,
            line.WarehouseId);
    }
}
