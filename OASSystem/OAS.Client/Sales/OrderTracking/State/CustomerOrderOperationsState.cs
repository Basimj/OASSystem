using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.OrderOperations;

namespace OAS.Client.Sales.OrderTracking.State;

public sealed class CustomerOrderOperationsState
{
    public CustomerOrderOperationsQueryRequest Query { get; set; } = new();
    public PagedResult<CustomerOrderOperationsItemDto> Page { get; set; } = new();
    public CustomerOrderOperationsSummaryDto? Summary { get; set; }
    public CustomerOrderOperationsDetailsDto? Selected { get; set; }
    public bool IsBusy { get; set; }
    public string? Error { get; set; }
}
