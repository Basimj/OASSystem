using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Stock;
using DomainStatus = OAS.Domain.Enums.Inventory.StockCountStatus;

namespace OAS.Application.Inventory.StockCounts.Services;

public interface IStockCountService
{
    Task<Guid> CreateAsync(CreateStockCountRequest request, CancellationToken cancellationToken = default);
    Task<Guid> StartAsync(Guid stockCountId, CancellationToken cancellationToken = default);
    Task<Guid> RecordCountAsync(Guid stockCountId, Guid lineId, RecordStockCountRequest request, CancellationToken cancellationToken = default);
    Task<Guid> CompleteAsync(Guid stockCountId, CancellationToken cancellationToken = default);
    Task<Guid> ApproveAsync(Guid stockCountId, CancellationToken cancellationToken = default);
    Task<Guid> PostAsync(Guid stockCountId, CancellationToken cancellationToken = default);
    Task<Guid> CancelAsync(Guid stockCountId, CancellationToken cancellationToken = default);
    Task<StockCountDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockCountLineDto>> GetLinesAsync(Guid stockCountId, CancellationToken cancellationToken = default);
    Task<PagedResult<StockCountDto>> GetPageAsync(
        PageRequest request,
        Guid? warehouseId = null,
        DomainStatus? status = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);
}
