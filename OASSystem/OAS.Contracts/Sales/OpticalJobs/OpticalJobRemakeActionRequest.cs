namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record OpticalJobRemakeActionRequest(
    string RowVersion,
    string RemakeRowVersion);
