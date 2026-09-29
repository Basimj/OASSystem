using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.CustomerOrders;

public sealed record UpdateCustomerOrderRequest(
    Guid CustomerId,
    Guid? PrescriptionRevisionId,
    DateOnly OrderDate,
    DateOnly? RequiredDate,
    Guid CurrencyId,
    TaxCalculationMode TaxCalculationMode,
    SalesPaymentTermType PaymentTermType,
    string? Notes,
    IReadOnlyList<CustomerOrderLineRequest> Lines,
    string RowVersion);
