namespace OAS.Contracts.Sales.SalesInvoices;

/// <summary>Authoritative currency conversion at the collection date, expressed in base currency.</summary>
public sealed record SalesInvoiceCollectionPreviewDto(
    bool CanCollect, decimal RequestedBaseAmount, decimal OutstandingBaseAmount,
    decimal ExpectedRemainingBaseAmount, string BaseCurrencyCode, string? Message);
