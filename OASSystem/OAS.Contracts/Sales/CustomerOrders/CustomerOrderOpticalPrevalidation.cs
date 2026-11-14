namespace OAS.Contracts.Sales.CustomerOrders;

/// <summary>Read-only optical product matching before creating/updating a customer order.</summary>
public sealed record CustomerOrderOpticalPrevalidationRequest(
    Guid? PrescriptionRevisionId,
    IReadOnlyList<CustomerOrderLineRequest> Lines);

public sealed record CustomerOrderOpticalPrevalidationIssue(
    int LineNumber,
    string Code,
    string Message);

public sealed record CustomerOrderOpticalPrevalidationResult(
    bool IsValid,
    IReadOnlyList<CustomerOrderOpticalPrevalidationIssue> Issues);
