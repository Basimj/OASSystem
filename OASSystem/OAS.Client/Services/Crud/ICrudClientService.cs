using OAS.Contracts.Common.Pagination;

namespace OAS.Client.Services.Crud;

public interface ICrudClientService<TKey, TReadDto, in TCreateDto, in TUpdateDto> where TKey : notnull
{
    Task<PagedResult<TReadDto>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<TReadDto> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<TReadDto> CreateAsync(TCreateDto request, CancellationToken cancellationToken = default);
    Task<TReadDto> UpdateAsync(TKey id, TUpdateDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}
