using OAS.Contracts.Purchasing.CustomerDemand;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.Checkout;

public sealed record CheckoutCustomerOrderResultDto(
    Guid CustomerOrderId,
    string OrderCode,
    CustomerOrderStatus OrderStatus,
    Guid? SalesInvoiceId,
    string? InvoiceCode,
    IReadOnlyList<Guid> ReceiptVoucherIds,
    IReadOnlyList<Guid> CustomerAdvanceIds,
    Guid? PurchaseRequestId,
    Guid? OpticalJobId,
    SalesPaymentSummaryDto PaymentSummary,
    IReadOnlyList<CustomerDemandQueueItemDto> Shortages);
