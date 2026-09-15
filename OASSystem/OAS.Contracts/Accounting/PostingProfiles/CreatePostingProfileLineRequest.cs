namespace OAS.Contracts.Accounting.PostingProfiles;

public sealed record CreatePostingProfileLineRequest(
    string AccountRole,
    Guid AccountId,
    bool IsRequired = true);