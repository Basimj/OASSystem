namespace OAS.Contracts.Accounting.PostingProfiles;

public sealed record CreatePostingProfileRequest(
    string Code,
    string Name,
    string Module,
    string DocumentType,
    IReadOnlyList<CreatePostingProfileLineRequest> Lines,
    bool IsActive = true);