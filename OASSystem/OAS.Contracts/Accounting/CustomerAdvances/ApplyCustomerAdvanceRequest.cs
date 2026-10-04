namespace OAS.Contracts.Accounting.CustomerAdvances;

public sealed record ApplyCustomerAdvanceRequest(
    Guid SalesInvoiceId,
    decimal Amount,
    string RowVersion);
