using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.CustomerOrders;

public sealed record CustomerOrderSummaryDto(
    Guid Id,
    string OrderCode,
    Guid CustomerId,
    string? CustomerCode,
    string? CustomerName,
    DateOnly OrderDate,
    DateOnly? RequiredDate,
    CustomerOrderStatus Status,
    string CurrencyCodeSnapshot,
    decimal TotalAmount,
    bool IsActive,
    string RowVersion)
{
    public SalesPaymentPlan? PaymentPlan { get; init; }
}
