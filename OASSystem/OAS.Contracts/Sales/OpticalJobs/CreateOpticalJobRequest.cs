namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record CreateOpticalJobRequest(
    Guid CustomerOrderId,
    Guid? SalesInvoiceId,
    DateOnly? RequiredDate,
    string? Notes,
    IReadOnlyList<CreateOpticalJobLineRequest> Lines);
