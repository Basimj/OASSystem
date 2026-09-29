using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.Prescriptions;

public sealed record SetPrescriptionStatusRequest(
    PrescriptionStatus Status,
    string RowVersion);
