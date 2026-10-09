using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.OpticalJobs;

namespace OAS.Client.Optical.Services;

public interface IOpticalJobsClientService
{
    Task<PagedResult<OpticalJobWorkQueueDto>> GetPageAsync(OpticalJobWorkQueueRequest request, CancellationToken ct = default);
    Task<OpticalJobDetailsDto?> GetDetailsAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<OpticalJobDetailsDto>> AssignAsync(Guid id, AssignOpticalJobRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OpticalJobDetailsDto>> IssueMaterialsAsync(Guid id, IssueOpticalJobMaterialsRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OpticalJobDetailsDto>> StartAsync(Guid id, OpticalJobActionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OpticalJobDetailsDto>> SendToQualityControlAsync(Guid id, OpticalJobActionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OpticalJobDetailsDto>> CompleteQualityControlAsync(Guid id, Guid qcId, CompleteOpticalQualityCheckRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OpticalJobDetailsDto>> PassQualityControlAsync(Guid id, OpticalJobActionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OpticalJobDetailsDto>> RecordBreakageAsync(Guid id, RecordOpticalJobBreakageRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OpticalJobDetailsDto>> CreateRemakeAsync(Guid id, CreateOpticalJobRemakeRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OpticalJobDetailsDto>> StartRemakeAsync(Guid id, Guid remakeId, OpticalJobRemakeActionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OpticalJobDetailsDto>> SendRemakeToQualityControlAsync(Guid id, Guid remakeId, OpticalJobRemakeActionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OpticalJobDetailsDto>> ReadyAsync(Guid id, OpticalJobActionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OpticalJobDetailsDto>> DeliverAsync(Guid id, OpticalJobActionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OpticalJobDetailsDto>> CancelAsync(Guid id, OpticalJobActionRequest request, CancellationToken ct = default);
}
