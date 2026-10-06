using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Lookups;
using OAS.Contracts.Sales.Prescriptions;

namespace OAS.Contracts.Sales.Checkout;

public sealed record SalesCheckoutContextDto(
    CustomerOrderDto Order,
    SalesCustomerLookupDto Customer,
    CustomerPrescriptionContextDto PrescriptionContext,
    CustomerOrderAvailabilityDto Availability,
    SalesCreditContextDto Credit,
    SalesPaymentSummaryDto PaymentSummary,
    Guid? SalesInvoiceId = null,
    string? InvoiceCode = null);
