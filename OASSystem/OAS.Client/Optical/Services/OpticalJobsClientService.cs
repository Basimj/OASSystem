using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.OpticalJobs;

namespace OAS.Client.Optical.Services;

public sealed class OpticalJobsClientService(OasApiClient api) : IOpticalJobsClientService
{
    public async Task<PagedResult<OpticalJobWorkQueueDto>> GetPageAsync(OpticalJobWorkQueueRequest r, CancellationToken ct = default)
    {
        var q = new List<string> { $"pageNumber={Math.Max(1, r.PageNumber)}", $"pageSize={Math.Clamp(r.PageSize, 1, PageRequest.MaximumPageSize)}" };
        Add(q, "search", r.Search); Add(q, "status", r.Status?.ToString()); Add(q, "technicianId", r.TechnicianId?.ToString());
        Add(q, "requiredDate", r.RequiredDate?.ToString("yyyy-MM-dd")); Add(q, "requiredDateFrom", r.RequiredDateFrom?.ToString("yyyy-MM-dd"));
        Add(q, "requiredDateTo", r.RequiredDateTo?.ToString("yyyy-MM-dd")); Add(q, "hasBreakage", r.HasBreakage?.ToString().ToLowerInvariant());
        Add(q, "hasRemake", r.HasRemake?.ToString().ToLowerInvariant()); Add(q, "sortBy", r.SortBy); Add(q, "sortDirection", r.SortDirection.ToString());
        return await api.GetAsync<PagedResult<OpticalJobWorkQueueDto>>("api/optical/jobs/work-queue?" + string.Join("&", q), ct) ?? new();
    }

    public Task<OpticalJobDetailsDto?> GetDetailsAsync(Guid id, CancellationToken ct = default) => api.GetAsync<OpticalJobDetailsDto>($"api/optical/jobs/{id:D}", ct);
    public Task<ApiCallResult<OpticalJobDetailsDto>> AssignAsync(Guid id, AssignOpticalJobRequest r, CancellationToken ct = default) => Post($"{id:D}/assign", r, ct);
    public Task<ApiCallResult<OpticalJobDetailsDto>> IssueMaterialsAsync(Guid id, IssueOpticalJobMaterialsRequest r, CancellationToken ct = default) => Post($"{id:D}/materials/issue", r, ct);
    public Task<ApiCallResult<OpticalJobDetailsDto>> StartAsync(Guid id, OpticalJobActionRequest r, CancellationToken ct = default) => Post($"{id:D}/start", r, ct);
    public Task<ApiCallResult<OpticalJobDetailsDto>> SendToQualityControlAsync(Guid id, OpticalJobActionRequest r, CancellationToken ct = default) => Post($"{id:D}/send-to-quality-control", r, ct);
    public Task<ApiCallResult<OpticalJobDetailsDto>> CompleteQualityControlAsync(Guid id, Guid qcId, CompleteOpticalQualityCheckRequest r, CancellationToken ct = default) => Post($"{id:D}/qc/{qcId:D}/complete", r, ct);
    public Task<ApiCallResult<OpticalJobDetailsDto>> PassQualityControlAsync(Guid id, OpticalJobActionRequest r, CancellationToken ct = default) => Post($"{id:D}/pass-quality-control", r, ct);
    public Task<ApiCallResult<OpticalJobDetailsDto>> RecordBreakageAsync(Guid id, RecordOpticalJobBreakageRequest r, CancellationToken ct = default) => Post($"{id:D}/breakages", r, ct);
    public Task<ApiCallResult<OpticalJobDetailsDto>> CreateRemakeAsync(Guid id, CreateOpticalJobRemakeRequest r, CancellationToken ct = default) => Post($"{id:D}/remakes", r, ct);
    public Task<ApiCallResult<OpticalJobDetailsDto>> StartRemakeAsync(Guid id, Guid remakeId, OpticalJobRemakeActionRequest r, CancellationToken ct = default) => Post($"{id:D}/remakes/{remakeId:D}/start", r, ct);
    public Task<ApiCallResult<OpticalJobDetailsDto>> SendRemakeToQualityControlAsync(Guid id, Guid remakeId, OpticalJobRemakeActionRequest r, CancellationToken ct = default) => Post($"{id:D}/remakes/{remakeId:D}/qc", r, ct);
    public Task<ApiCallResult<OpticalJobDetailsDto>> ReadyAsync(Guid id, OpticalJobActionRequest r, CancellationToken ct = default) => Post($"{id:D}/ready-for-delivery", r, ct);
    public Task<ApiCallResult<OpticalJobDetailsDto>> DeliverAsync(Guid id, OpticalJobActionRequest r, CancellationToken ct = default) => Post($"{id:D}/deliver", r, ct);
    public Task<ApiCallResult<OpticalJobDetailsDto>> CancelAsync(Guid id, OpticalJobActionRequest r, CancellationToken ct = default) => Post($"{id:D}/cancel", r, ct);

    private Task<ApiCallResult<OpticalJobDetailsDto>> Post<T>(string path, T request, CancellationToken ct) =>
        api.PostResultAsync<T, OpticalJobDetailsDto>($"api/optical/jobs/{path}", request, ct);

    private static void Add(List<string> q, string key, string? value) { if (!string.IsNullOrWhiteSpace(value)) q.Add($"{key}={Uri.EscapeDataString(value)}"); }
}
