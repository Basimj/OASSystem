namespace OAS.Contracts.Database;

public sealed record DatabaseUpdateStatusDto(
    string ProfileKey,
    bool CanConnect,
    bool IsUpToDate,
    int PendingCount,
    string? ErrorCode = null);
