namespace OAS.Contracts.Sales.Prescriptions;

public sealed record CreatePrescriptionInitialRevisionRequest(
    DateOnly EffectiveDate,
    string? Reason,
    IReadOnlyList<PrescriptionEyeDetailRequest> EyeDetails);
