namespace OAS.Contracts.Accounting.Settings;

/// <summary>The control account that receives newly created customer sub-accounts.</summary>
public sealed record CustomerReceivablesSettingsDto(
    Guid? ParentAccountId,
    string? ParentAccountCode,
    string? ParentAccountName);

public sealed record UpdateCustomerReceivablesSettingsRequest(Guid? ParentAccountId);
