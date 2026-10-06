namespace OAS.Contracts.Sales.Checkout;

public sealed record SalesCreditContextDto(
    Guid CustomerId,
    string CustomerCode,
    bool IsActive,
    bool IsCreditAllowed,
    decimal CreditLimit,
    decimal CurrentCreditExposure,
    decimal AvailableCredit,
    int PaymentTermDays);
