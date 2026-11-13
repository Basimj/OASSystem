using OAS.Contracts.Common.Pagination;
using OAS.UiLib.Core.Models.Sales;
using OAS.Contracts.Sales.OrderOperations;

namespace OAS.Client.Sales.OrderTracking.State;

public sealed class CustomerOrderOperationsState
{
    public UiOrderOperationsFilterModel Filter { get; } = new();
    public DateTimeOffset? LastUpdatedAt { get; set; }
    public CustomerOrderOperationsQueryRequest Query { get; set; } = new() { PageSize = 10 };
    public PagedResult<CustomerOrderOperationsItemDto> Page { get; set; } = new();
    public CustomerOrderOperationsSummaryDto? Summary { get; set; }
    public CustomerOrderOperationsDetailsDto? Selected { get; set; }
    public bool IsBusy { get; set; }
    public string? Error { get; set; }
}
