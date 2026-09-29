using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Purchasing.PurchaseRequests;

public sealed record PurchaseRequestLineAllocationRequest(
    Guid PurchaseRequestLineId,
    decimal AllocatedQuantity);

public sealed record CreatePurchaseOrderFromRequestRequest(
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
    IReadOnlyList<PurchaseRequestLineAllocationRequest> Allocations,
    string RowVersion);
