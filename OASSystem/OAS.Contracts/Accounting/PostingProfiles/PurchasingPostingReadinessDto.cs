namespace OAS.Contracts.Accounting.PostingProfiles;

/// <summary>Server-computed, read-only diagnostic; does not grant permission to post.</summary>
public sealed record PurchasingPostingReadinessIssueDto(
    string Code,
    string Severity,
    string Message,
    string? DocumentType = null,
    string? Role = null);

public sealed record PurchasingPostingReadinessDto(
    DateOnly PostingDate,
    Guid? SupplierId,
    string? BaseCurrencyCode,
    bool IsReady,
    IReadOnlyList<PurchasingPostingReadinessIssueDto> Issues);
