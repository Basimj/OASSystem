namespace OAS.Contracts.Purchasing.PurchaseInvoices;

/// <summary>Receipt quantity still eligible for matching the specified invoice.
/// Accounts for posted returns and allocations belonging to OTHER invoices.</summary>
public sealed record PurchaseReceiptMatchAvailabilityDto(
    Guid ReceiptLineId,
    decimal AvailableQuantity);
