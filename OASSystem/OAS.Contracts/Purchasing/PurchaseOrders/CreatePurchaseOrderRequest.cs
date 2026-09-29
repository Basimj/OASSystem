using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Purchasing.PurchaseOrders;

public sealed record CreatePurchaseOrderRequest(
    Guid SupplierId,
    Guid DestinationWarehouseId,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    Guid CurrencyId,
    decimal ExchangeRate,
    DateOnly ExchangeRateDate,
    TaxCalculationMode TaxCalculationMode,
    int PaymentTermDays,
    string? Notes,
    IReadOnlyList<CreatePurchaseOrderLineRequest> Lines);
