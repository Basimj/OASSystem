namespace OAS.Contracts.Sales.OrderOperations;

public sealed record CustomerOrderOperationalTimelineItemDto(
    string EventType,
    string DisplayText,
    DateTimeOffset OccurredAt,
    string? Actor,
    Guid? SourceDocumentId,
    string? SourceDocumentCode);
