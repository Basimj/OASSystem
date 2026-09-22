using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.Journals;

public sealed record JournalEntryDto(
    Guid Id,
    string JournalNumber,
    JournalType JournalType,
    DateOnly PostingDate,
    DateOnly DocumentDate,
    Guid FiscalPeriodId,
    string Description,
    string? SourceModule,
    string? SourceDocumentType,
    Guid? SourceDocumentId,
    JournalEntryStatus Status,
    Guid? ReversedJournalId,
    string? CreatedBy,
    DateTimeOffset CreatedAtUtc,
    Guid? ApprovedBy,
    DateTimeOffset? ApprovedAtUtc,
    Guid? PostedBy,
    DateTimeOffset? PostedAtUtc,
    string RowVersion,
    IReadOnlyList<JournalEntryLineDto> Lines);
