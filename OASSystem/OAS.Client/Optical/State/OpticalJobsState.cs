using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.OpticalJobs;

namespace OAS.Client.Optical.State;

public sealed class OpticalJobsState
{
    public OpticalJobWorkQueueRequest Query { get; set; } = new();
    public PagedResult<OpticalJobWorkQueueDto> Page { get; set; } = new();
    public OpticalJobDetailsDto? Selected { get; set; }
    public bool IsBusy { get; set; }
    public string? Error { get; set; }
    public string? Success { get; set; }
}
