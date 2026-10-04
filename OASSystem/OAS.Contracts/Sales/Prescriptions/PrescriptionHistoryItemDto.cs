using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.Prescriptions;

public sealed record PrescriptionHistoryItemDto(
    Guid PrescriptionId,
    string PrescriptionCode,
    DateOnly PrescriptionDate,
    PrescriptionStatus PrescriptionStatus,
    Guid RevisionId,
    int RevisionNumber,
    DateOnly EffectiveDate,
    string? PrescribedBy,
    string? ClinicName,
    bool IsLatest,
    bool IsCurrent,
    CustomerPrescriptionEyeContextDto? OD,
    CustomerPrescriptionEyeContextDto? OS);
