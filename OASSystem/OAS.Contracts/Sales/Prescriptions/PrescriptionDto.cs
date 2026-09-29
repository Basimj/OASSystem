using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.Prescriptions;

public sealed record PrescriptionDto(
    Guid Id,
    string PrescriptionCode,
    Guid CustomerId,
    string? CustomerCode,
    string? CustomerName,
    DateOnly PrescriptionDate,
    PrescriptionStatus Status,
    string? PrescribedBy,
    string? ClinicName,
    string? Notes,
    bool IsActive,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedAtUtc,
    string? LastModifiedBy,
    IReadOnlyList<PrescriptionRevisionDto> Revisions);
