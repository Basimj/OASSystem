namespace OAS.Contracts.Accounting.PostingProfiles;

public sealed record UpdatePostingProfileRequest(
    string Code,
    string Name,
    string Module,
    string DocumentType,
    IReadOnlyList<CreatePostingProfileLineRequest> Lines,
    bool IsActive,
    string RowVersion);