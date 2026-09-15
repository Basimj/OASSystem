namespace OAS.Contracts.Accounting.PaymentAllocations;

public sealed record UpdatePaymentAllocationRequest(
    decimal AllocatedAmount);