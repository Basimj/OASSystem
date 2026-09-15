namespace OAS.Domain.Accounting.Enums;

public enum JournalType : byte
{
    Automatic = 1,
    Manual = 2,
    Opening = 3,
    Closing = 4,
    Reversal = 5
}