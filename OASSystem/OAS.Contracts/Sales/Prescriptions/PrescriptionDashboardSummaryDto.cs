namespace OAS.Contracts.Sales.Prescriptions;

/// <summary>Global prescription counts, independent of pagination or search.</summary>
public sealed record PrescriptionDashboardSummaryDto(
    long TotalPrescriptions,
    long DraftPrescriptions,
    long ActivePrescriptions,
    long CancelledPrescriptions,
    long TotalRevisions);
