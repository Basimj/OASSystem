namespace OAS.Contracts.Accounting.Enums;
public enum JournalEntryStatus : byte
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Posted = 4,
    Reversed = 5
}