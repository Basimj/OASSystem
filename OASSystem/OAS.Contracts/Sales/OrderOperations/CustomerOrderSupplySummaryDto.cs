namespace OAS.Contracts.Sales.OrderOperations;

public sealed record CustomerOrderSupplySummaryDto(
    bool HasShortage,
    int NotOrderedLines,
    int ScheduledLines,
    int OrderedLines,
    int DueTodayLines,
    int OverdueLines,
    int PartiallyReceivedLines,
    int ReceivedLines,
    string? SummaryText);
