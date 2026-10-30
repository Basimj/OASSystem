namespace OAS.Contracts.Accounting.Accounts;

/// <summary>
/// Non-mutating pre-save advisory check for chart-of-accounts editing.
/// The update handler remains the final authority for all business rules.
/// </summary>
public sealed record AccountSaveCheckDto(bool CanSave, string? ReasonCode);
