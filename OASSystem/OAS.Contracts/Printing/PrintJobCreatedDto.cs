namespace OAS.Contracts.Printing;

public sealed record PrintJobCreatedDto(
    Guid JobId,
    string WorkstationCode,
    DateTimeOffset QueuedAtUtc);
