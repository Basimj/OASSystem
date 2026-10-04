namespace OAS.Contracts.Accounting.CustomerAdvances;

public sealed record CreateCustomerAdvanceRequest(
    Guid CustomerId,
    Guid CustomerOrderId,
    Guid ReceiptVoucherLineId);
