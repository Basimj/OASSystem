using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;

namespace OAS.Client.Services.Crud;

public abstract class CrudClientService<TKey, TReadDto, TCreateDto, TUpdateDto>(OasApiClient apiClient, string endpoint)
    : ICrudClientService<TKey, TReadDto, TCreateDto, TUpdateDto> where TKey : notnull
{
    protected string Endpoint { get; } = endpoint.TrimEnd('/');

    public async Task<PagedResult<TReadDto>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        var q = request.Normalize();
        var uri = $"{Endpoint}?pageNumber={q.PageNumber}&pageSize={q.PageSize}";
        if (!string.IsNullOrWhiteSpace(q.Search)) uri += $"&search={Uri.EscapeDataString(q.Search)}";
        if (!string.IsNullOrWhiteSpace(q.SortBy)) uri += $"&sortBy={Uri.EscapeDataString(q.SortBy)}&sortDirection={q.SortDirection}";
        return await apiClient.GetAsync<PagedResult<TReadDto>>(uri, cancellationToken) ?? new PagedResult<TReadDto>();
    }

    public async Task<TReadDto> GetByIdAsync(TKey id, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<TReadDto>($"{Endpoint}/{Uri.EscapeDataString(id.ToString()!)}", cancellationToken)
        ?? throw new InvalidOperationException("The API returned an empty response.");

    public async Task<TReadDto> CreateAsync(TCreateDto request, CancellationToken cancellationToken = default) =>
        await apiClient.PostAsync<TCreateDto, TReadDto>(Endpoint, request, cancellationToken)
        ?? throw new InvalidOperationException("The API returned an empty response.");

    public async Task<TReadDto> UpdateAsync(TKey id, TUpdateDto request, CancellationToken cancellationToken = default) =>
        await apiClient.PutAsync<TUpdateDto, TReadDto>($"{Endpoint}/{Uri.EscapeDataString(id.ToString()!)}", request, cancellationToken)
        ?? throw new InvalidOperationException("The API returned an empty response.");

    public Task DeleteAsync(TKey id, CancellationToken cancellationToken = default) =>
        apiClient.DeleteAsync($"{Endpoint}/{Uri.EscapeDataString(id.ToString()!)}", cancellationToken);
}
