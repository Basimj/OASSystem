using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.Journals;

public sealed record UpdateJournalEntryRequest(
    JournalType JournalType,
    DateOnly PostingDate,
    DateOnly DocumentDate,
    Guid FiscalPeriodId,
    string Description,
    string? SourceModule,
    string? SourceDocumentType,
    Guid? SourceDocumentId,
    IReadOnlyList<CreateJournalEntryLineRequest> Lines,
    string RowVersion);