using OAS.Contracts.Sales.Common;

namespace OAS.Contracts.Sales.SalesInvoices;

public sealed record CollectSalesInvoicePaymentResultDto(
    Guid InvoiceId,
    Guid ReceiptVoucherId,
    string ReceiptVoucherNumber,
    SalesInvoicePaymentSummaryDto PaymentSummary);
