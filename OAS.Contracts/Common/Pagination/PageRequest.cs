namespace OAS.Contracts.Common.Pagination;

public sealed record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 200;

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;

    public PageRequest Normalize() => this with
    {
        PageNumber = Math.Max(1, PageNumber),
        PageSize = Math.Clamp(PageSize, 1, MaximumPageSize),
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        SortBy = string.IsNullOrWhiteSpace(SortBy) ? null : SortBy.Trim()
    };
}
