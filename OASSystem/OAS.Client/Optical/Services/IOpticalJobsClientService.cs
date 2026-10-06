using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.OpticalJobs;

namespace OAS.Client.Optical.Services;

public interface IOpticalJobsClientService
{
    Task<PagedResult<OpticalJobWorkQueueDto>> GetPageAsync(OpticalJobWorkQueueRequest request, CancellationToken ct = default);
    Task<OpticalJobDetailsDto?> GetDetailsAsync(Guid id, CancellationToken ct = default);
    Task<OpticalJobDetailsDto?> AssignAsync(Guid id, AssignOpticalJobRequest request, CancellationToken ct = default);
    Task<OpticalJobDetailsDto?> StartAsync(Guid id, OpticalJobActionRequest request, CancellationToken ct = default);
    Task<OpticalJobDetailsDto?> ReadyAsync(Guid id, OpticalJobActionRequest request, CancellationToken ct = default);
}
