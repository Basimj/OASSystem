using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.SalesInvoices;

public sealed record PostSalesInvoiceRequest(
    string RowVersion,
    SalesImmediatePaymentMethod? ImmediatePaymentMethod = null,
    Guid? CashAccountId = null,
    Guid? BankAccountId = null);
