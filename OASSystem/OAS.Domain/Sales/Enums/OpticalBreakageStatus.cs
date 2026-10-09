namespace OAS.Domain.Sales.Enums;

public enum OpticalBreakageStatus : byte
{
    Recorded = 1,
    ReplacementAvailable = 2,
    AwaitingReplacement = 3,
    ReplacementReceived = 4,
    Closed = 5,
    Cancelled = 6
}
