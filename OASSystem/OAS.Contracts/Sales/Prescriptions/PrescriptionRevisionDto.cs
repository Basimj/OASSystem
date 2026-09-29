namespace OAS.Contracts.Sales.Prescriptions;

public sealed record PrescriptionRevisionDto(
    Guid Id,
    Guid PrescriptionId,
    int RevisionNumber,
    DateOnly EffectiveDate,
    string? Reason,
    bool IsCurrent,
    bool IsActive,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedAtUtc,
    string? LastModifiedBy,
    IReadOnlyList<PrescriptionEyeDetailDto> EyeDetails);
