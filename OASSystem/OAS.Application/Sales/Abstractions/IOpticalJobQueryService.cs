using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.OpticalJobs;

namespace OAS.Application.Sales.Abstractions;

public interface IOpticalJobQueryService
{
    Task<PagedResult<OpticalJobWorkQueueDto>> GetWorkQueueAsync(
        OpticalJobWorkQueueRequest request,
        CancellationToken cancellationToken = default);

    Task<OpticalJobDetailsDto> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
