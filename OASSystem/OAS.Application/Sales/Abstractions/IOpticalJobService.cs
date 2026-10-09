using OAS.Contracts.Sales.OpticalJobs;

namespace OAS.Application.Sales.Abstractions;

public interface IOpticalJobService
{
    Task<Guid> CreateAsync(CreateOpticalJobRequest request, CancellationToken cancellationToken = default);
    Task AssignAsync(Guid id, AssignOpticalJobRequest request, CancellationToken cancellationToken = default);
    Task IssueMaterialsAsync(Guid id, IssueOpticalJobMaterialsRequest request, CancellationToken cancellationToken = default);
    Task StartAsync(Guid id, OpticalJobActionRequest request, CancellationToken cancellationToken = default);
    Task SendToQualityControlAsync(Guid id, OpticalJobActionRequest request, CancellationToken cancellationToken = default);
    Task CompleteQualityControlAsync(Guid id, Guid qualityCheckId, CompleteOpticalQualityCheckRequest request, CancellationToken cancellationToken = default);
    Task PassQualityControlAsync(Guid id, OpticalJobActionRequest request, CancellationToken cancellationToken = default);
    Task<Guid> RecordBreakageAsync(Guid id, RecordOpticalJobBreakageRequest request, CancellationToken cancellationToken = default);
    Task<Guid> CreateRemakeAsync(Guid id, CreateOpticalJobRemakeRequest request, CancellationToken cancellationToken = default);
    Task StartRemakeAsync(Guid id, Guid remakeId, OpticalJobRemakeActionRequest request, CancellationToken cancellationToken = default);
    Task SendRemakeToQualityControlAsync(Guid id, Guid remakeId, OpticalJobRemakeActionRequest request, CancellationToken cancellationToken = default);
    Task MarkReadyAsync(Guid id, OpticalJobActionRequest request, CancellationToken cancellationToken = default);
    Task DeliverAsync(Guid id, OpticalJobActionRequest request, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, OpticalJobActionRequest request, CancellationToken cancellationToken = default);
    Task<OpticalJobDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OpticalJobWorkQueueDto>> GetWorkQueueAsync(CancellationToken cancellationToken = default);
}
