using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.Journals;

public sealed record CreateJournalEntryRequest(
    JournalType JournalType,
    DateOnly PostingDate,
    DateOnly DocumentDate,
    Guid FiscalPeriodId,
    string Description,
    string? SourceModule,
    string? SourceDocumentType,
    Guid? SourceDocumentId,
    IReadOnlyList<CreateJournalEntryLineRequest> Lines,
    string? JournalNumber = null);