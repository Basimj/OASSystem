namespace OAS.Contracts.Sales.Prescriptions;

public sealed record CustomerPrescriptionContextDto(
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    string? Mobile,
    bool HasPrescription,
    Guid? LatestPrescriptionId,
    string? LatestPrescriptionCode,
    Guid? LatestRevisionId,
    int? LatestRevisionNumber,
    DateOnly? LatestExamDate,
    CustomerPrescriptionEyeContextDto? OD,
    CustomerPrescriptionEyeContextDto? OS);
