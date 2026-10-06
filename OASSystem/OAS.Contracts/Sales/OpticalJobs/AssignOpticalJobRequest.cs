namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record AssignOpticalJobRequest(
    Guid? TechnicianId,
    string RowVersion);
