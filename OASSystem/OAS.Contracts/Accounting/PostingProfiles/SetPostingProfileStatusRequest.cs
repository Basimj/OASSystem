namespace OAS.Contracts.Accounting.PostingProfiles;

public sealed record SetPostingProfileStatusRequest(
    bool IsActive,
    string RowVersion);