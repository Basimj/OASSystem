namespace OAS.Contracts.Common.Pagination;

public sealed record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 200;

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;
    public string? Search { get; init; }
    // Optional server-side status filter used by multiple accounting list endpoints.
    public string? Status { get; init; }
    // Expense-specific filters. Other page specifications intentionally ignore these.
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public Guid? ExpenseTypeId { get; init; }
    // Fiscal-period directory scope. Ignored by unrelated list endpoints.
    public Guid? FiscalYearId { get; init; }
    public int? PaymentMethod { get; init; }
    public string? SortBy { get; init; }
    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;

    public PageRequest Normalize() => this with
    {
        PageNumber = Math.Max(1, PageNumber),
        PageSize = Math.Clamp(PageSize, 1, MaximumPageSize),
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        Status = string.IsNullOrWhiteSpace(Status) ? null : Status.Trim().ToLowerInvariant(),
        SortBy = string.IsNullOrWhiteSpace(SortBy) ? null : SortBy.Trim()
    };
}
