using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OrderOperations;

public sealed record CustomerOrderOperationsQueryRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = PageRequest.DefaultPageSize;
    public string? Search { get; init; }
    public DateOnly? OrderDateFrom { get; init; }
    public DateOnly? OrderDateTo { get; init; }
    public CustomerOrderStatus? Status { get; init; }
    public SalesLineType? ProductType { get; init; }
    public Guid? WarehouseId { get; init; }
    public bool? RequiresProduction { get; init; }
    public CustomerOrderLineAvailabilityState? Availability { get; init; }
    public DateOnly? RequiredDate { get; init; }
    public bool OverdueOnly { get; init; }
    public Guid? TechnicianId { get; init; }
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
