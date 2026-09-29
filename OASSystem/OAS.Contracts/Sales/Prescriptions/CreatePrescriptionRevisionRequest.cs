namespace OAS.Contracts.Sales.Prescriptions;

public sealed record CreatePrescriptionRevisionRequest(
    DateOnly EffectiveDate,
    string? Reason,
    IReadOnlyList<PrescriptionEyeDetailRequest> EyeDetails,
    string PrescriptionRowVersion);
