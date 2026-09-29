using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.Lookups;

public sealed record CustomerOrderLookupDto(
    Guid Id,
    string OrderCode,
    Guid CustomerId,
    DateOnly OrderDate,
    CustomerOrderStatus Status,
    decimal TotalAmount,
    string CurrencyCode,
    string RowVersion);
