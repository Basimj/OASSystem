namespace OAS.Contracts.Sales.Common;

public sealed record SalesInvoicePaymentSummaryDto(
    Guid InvoiceId,
    decimal InvoiceTotal,
    decimal PaidAmount,
    decimal OutstandingAmount);
