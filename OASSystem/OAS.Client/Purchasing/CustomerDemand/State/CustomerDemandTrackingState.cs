using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.CustomerDemand;

namespace OAS.Client.Purchasing.CustomerDemand.State;

public sealed class CustomerDemandTrackingState
{
    public CustomerDemandTrackingQueryRequest Query { get; set; } = new();
    public PagedResult<CustomerDemandTrackingItemDto> Page { get; set; } = new();
    public CustomerDemandTrackingSummaryDto? Summary { get; set; }
    public CustomerDemandTrackingDetailsDto? Selected { get; set; }
    public bool IsBusy { get; set; }
    public string? Error { get; set; }
    public string? Success { get; set; }
}
