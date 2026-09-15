namespace OAS.Contracts.Accounting.PostingProfiles;

public sealed record PostingProfileLineDto(
    Guid Id,
    Guid PostingProfileId,
    string AccountRole,
    Guid AccountId,
    bool IsRequired);