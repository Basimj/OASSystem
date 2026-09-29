using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.Prescriptions;

public sealed record PrescriptionSummaryDto(
    Guid Id,
    string PrescriptionCode,
    Guid CustomerId,
    string? CustomerCode,
    string? CustomerName,
    DateOnly PrescriptionDate,
    PrescriptionStatus Status,
    string? PrescribedBy,
    string? ClinicName,
    bool IsActive,
    int? CurrentRevisionNumber,
    Guid? CurrentRevisionId,
    string RowVersion);
