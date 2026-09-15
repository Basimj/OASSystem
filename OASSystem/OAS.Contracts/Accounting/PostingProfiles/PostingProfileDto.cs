namespace OAS.Contracts.Accounting.PostingProfiles;

public sealed record PostingProfileDto(
    Guid Id,
    string Code,
    string Name,
    string Module,
    string DocumentType,
    bool IsActive,
    string RowVersion,
    IReadOnlyList<PostingProfileLineDto> Lines);