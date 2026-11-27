namespace OAS.Contracts.Accounting.Journals;

public sealed record ReverseJournalEntryRequest(DateOnly PostingDate, string Reason);
