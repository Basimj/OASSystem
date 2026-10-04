using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.SalesInvoices;

public sealed record UpdateSalesInvoiceRequest(
    Guid CustomerId,
    Guid? PrescriptionRevisionId,
    DateOnly InvoiceDate,
    DateOnly PostingDate,
    Guid CurrencyId,
    TaxCalculationMode TaxCalculationMode,
    SalesPaymentTermType PaymentTermType,
    string? Description,
    IReadOnlyList<SalesInvoiceLineRequest> Lines,
    string RowVersion,
    Guid? SalesEmployeeId = null);
