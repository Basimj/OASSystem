namespace OAS.Contracts.Sales.Prescriptions;

public sealed record CreatePrescriptionRequest(
    string PrescriptionCode,
    Guid CustomerId,
    DateOnly PrescriptionDate,
    string? PrescribedBy,
    string? ClinicName,
    string? Notes);
