using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.Journals;

public sealed record SetJournalEntryStatusRequest(
    JournalEntryStatus Status,
    string RowVersion);