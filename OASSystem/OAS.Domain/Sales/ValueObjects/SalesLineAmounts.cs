namespace OAS.Domain.Sales.ValueObjects;

public readonly record struct SalesLineAmounts(
    decimal GrossAmount,
    decimal DiscountAmount,
    decimal NetAmount,
    decimal TaxAmount,
    decimal FinalAmount);
