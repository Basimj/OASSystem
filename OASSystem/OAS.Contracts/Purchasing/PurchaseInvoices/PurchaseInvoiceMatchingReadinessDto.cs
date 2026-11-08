namespace OAS.Contracts.Purchasing.PurchaseInvoices;

/// <summary>
/// Read-only status of the receipt/invoice matching prerequisite.
/// This is not an authorization to post: all posting rules are checked again by the command.
/// </summary>
public sealed record PurchaseInvoiceMatchingReadinessDto(
    bool IsComplete,
    string Code,
    string Message,
    int AllocationCount,
    int UnresolvedCount,
    IReadOnlyList<PurchaseInvoiceReceiptAllocationDto> Allocations);
