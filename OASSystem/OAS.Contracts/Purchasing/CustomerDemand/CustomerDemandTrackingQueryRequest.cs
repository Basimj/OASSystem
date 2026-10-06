using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Purchasing.CustomerDemand;

public sealed record CustomerDemandTrackingQueryRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = PageRequest.DefaultPageSize;
    public string? Search { get; init; }
    public DateOnly? RequestDateFrom { get; init; }
    public DateOnly? RequestDateTo { get; init; }
    public Guid? SupplierId { get; init; }
    public CustomerDemandTrackingStatus? Status { get; init; }
    public SalesLineType? ProductType { get; init; }
    public Guid? WarehouseId { get; init; }
    public DateOnly? ExpectedDeliveryDate { get; init; }
    public Guid? CustomerOrderId { get; init; }
    public Guid? CustomerId { get; init; }
    public bool OverdueOnly { get; init; }
    public EyeSide? Eye { get; init; }
    public bool? RequiresProduction { get; init; }
    public PurchaseOrderStatus? PurchaseOrderStatus { get; init; }
    public PurchaseReceiptStatus? ReceiptStatus { get; init; }
    public string? SortBy { get; init; }
    public SortDirection SortDirection { get; init; } = SortDirection.Descending;

    public PageRequest ToPageRequest() => new()
    {
        PageNumber = Math.Max(1, PageNumber),
        PageSize = Math.Clamp(PageSize, 1, PageRequest.MaximumPageSize),
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        SortBy = string.IsNullOrWhiteSpace(SortBy) ? null : SortBy.Trim(),
        SortDirection = SortDirection
    };
}
