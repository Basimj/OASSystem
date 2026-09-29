namespace OAS.Contracts.Sales.Prescriptions;

public sealed record UpdatePrescriptionRequest(
    DateOnly PrescriptionDate,
    string? PrescribedBy,
    string? ClinicName,
    string? Notes,
    string RowVersion);
