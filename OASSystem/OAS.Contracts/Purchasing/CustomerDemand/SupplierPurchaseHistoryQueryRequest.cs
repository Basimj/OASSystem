using OAS.Contracts.Common.Pagination;

namespace OAS.Contracts.Purchasing.CustomerDemand;

public sealed record SupplierPurchaseHistoryQueryRequest
{
    public Guid SupplierId { get; init; }
    public Guid? ProductVariantId { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = PageRequest.DefaultPageSize;

    public PageRequest ToPageRequest() => new()
    {
        PageNumber = Math.Max(1, PageNumber),
        PageSize = Math.Clamp(PageSize, 1, PageRequest.MaximumPageSize),
        SortBy = "OrderDate",
        SortDirection = SortDirection.Descending
    };
}
