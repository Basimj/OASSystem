using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.OpticalJobs;

namespace OAS.Client.Optical.Services;

public sealed class OpticalJobsClientService(OasApiClient api) : IOpticalJobsClientService
{
    public async Task<PagedResult<OpticalJobWorkQueueDto>> GetPageAsync(OpticalJobWorkQueueRequest r, CancellationToken ct = default)
    {
        var q=new List<string>{$"pageNumber={Math.Max(1,r.PageNumber)}",$"pageSize={Math.Clamp(r.PageSize,1,PageRequest.MaximumPageSize)}"};
        Add(q,"search",r.Search); Add(q,"status",r.Status?.ToString()); Add(q,"technicianId",r.TechnicianId?.ToString()); Add(q,"requiredDate",r.RequiredDate?.ToString("yyyy-MM-dd")); Add(q,"sortBy",r.SortBy); Add(q,"sortDirection",r.SortDirection.ToString());
        return await api.GetAsync<PagedResult<OpticalJobWorkQueueDto>>("api/optical/jobs/work-queue?"+string.Join("&",q),ct) ?? new();
    }
    public Task<OpticalJobDetailsDto?> GetDetailsAsync(Guid id,CancellationToken ct=default)=>api.GetAsync<OpticalJobDetailsDto>($"api/optical/jobs/{id:D}",ct);
    public Task<OpticalJobDetailsDto?> AssignAsync(Guid id,AssignOpticalJobRequest r,CancellationToken ct=default)=>api.PostAsync<AssignOpticalJobRequest,OpticalJobDetailsDto>($"api/optical/jobs/{id:D}/assign",r,ct);
    public Task<OpticalJobDetailsDto?> StartAsync(Guid id,OpticalJobActionRequest r,CancellationToken ct=default)=>api.PostAsync<OpticalJobActionRequest,OpticalJobDetailsDto>($"api/optical/jobs/{id:D}/start",r,ct);
    public Task<OpticalJobDetailsDto?> ReadyAsync(Guid id,OpticalJobActionRequest r,CancellationToken ct=default)=>api.PostAsync<OpticalJobActionRequest,OpticalJobDetailsDto>($"api/optical/jobs/{id:D}/ready-for-delivery",r,ct);
    private static void Add(List<string> q,string key,string? value){if(!string.IsNullOrWhiteSpace(value))q.Add($"{key}={Uri.EscapeDataString(value)}");}
}
