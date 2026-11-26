using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.Journals;

// Independent of posting status. Historic ledger movements remain posted.
public sealed record JournalReversalInfoDto(
    bool IsReversal,
    Guid? OriginalJournalId,
    string? OriginalJournalNumber,
    Guid? ReversalJournalId,
    string? ReversalJournalNumber,
    JournalEntryStatus? ReversalJournalStatus);
