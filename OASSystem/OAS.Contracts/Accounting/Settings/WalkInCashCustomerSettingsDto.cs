namespace OAS.Contracts.Accounting.Settings;

/// <summary>
/// Shared, registered cash customer used for fully-paid anonymous sales.
/// The customer's associated receivables account is read-only information.
/// </summary>
public sealed record WalkInCashCustomerSettingsDto(
    Guid? CustomerId,
    string? CustomerCode,
    string? CustomerName,
    string? ReceivablesAccountCode)
{
    // Kept for wire compatibility with the previously deployed settings page.
    public Guid? ReceivablesAccountId { get; init; }
    public string? ReceivablesAccountName { get; init; }
}

/// <summary>
/// Choose a customer from Accounting/Customers. The linked accounting account
/// is resolved server-side; it must not be chosen independently.
/// </summary>
public sealed record UpdateWalkInCashCustomerSettingsRequest(
    Guid? CustomerId,
    Guid? ReceivablesAccountId = null);
