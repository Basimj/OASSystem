using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record OpticalJobWorkQueueRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = PageRequest.DefaultPageSize;
    public string? Search { get; init; }
    public OpticalJobStatus? Status { get; init; }
    public Guid? TechnicianId { get; init; }
    public DateOnly? RequiredDate { get; init; }
    public string? SortBy { get; init; }
    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;

    public PageRequest ToPageRequest() => new()
    {
        PageNumber = Math.Max(1, PageNumber),
        PageSize = Math.Clamp(PageSize, 1, PageRequest.MaximumPageSize),
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        SortBy = string.IsNullOrWhiteSpace(SortBy) ? null : SortBy.Trim(),
        SortDirection = SortDirection
    };
}
