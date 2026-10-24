namespace OAS.Contracts.Accounting.Settings;

/// <summary>Shared, registered cash customer used for anonymous walk-in sales.</summary>
public sealed record WalkInCashCustomerSettingsDto(
    Guid? CustomerId,
    string? CustomerCode,
    string? CustomerName,
    string? ReceivablesAccountCode);

public sealed record UpdateWalkInCashCustomerSettingsRequest(Guid? CustomerId);
