using MediatR;
using OAS.Application.Inventory.StockCounts.Commands.ApproveStockCount;
using OAS.Application.Inventory.StockCounts.Commands.CancelStockCount;
using OAS.Application.Inventory.StockCounts.Commands.CompleteStockCount;
using OAS.Application.Inventory.StockCounts.Commands.CreateStockCount;
using OAS.Application.Inventory.StockCounts.Commands.PostStockCount;
using OAS.Application.Inventory.StockCounts.Commands.RecordStockCount;
using OAS.Application.Inventory.StockCounts.Commands.StartStockCount;
using OAS.Application.Inventory.StockCounts.Queries.GetStockCountById;
using OAS.Application.Inventory.StockCounts.Queries.GetStockCountLines;
using OAS.Application.Inventory.StockCounts.Queries.GetStockCounts;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Stock;
using DomainStatus = OAS.Domain.Enums.Inventory.StockCountStatus;

namespace OAS.Application.Inventory.StockCounts.Services;

public sealed class StockCountService(ISender sender) : IStockCountService
{
    public Task<Guid> CreateAsync(CreateStockCountRequest request, CancellationToken cancellationToken = default) =>
        sender.Send(new CreateStockCountCommand(request), cancellationToken);

    public Task<Guid> StartAsync(Guid stockCountId, CancellationToken cancellationToken = default) =>
        sender.Send(new StartStockCountCommand(stockCountId), cancellationToken);

    public Task<Guid> RecordCountAsync(Guid stockCountId, Guid lineId, RecordStockCountRequest request, CancellationToken cancellationToken = default) =>
        sender.Send(new RecordStockCountCommand(stockCountId, lineId, request), cancellationToken);

    public Task<Guid> CompleteAsync(Guid stockCountId, CancellationToken cancellationToken = default) =>
        sender.Send(new CompleteStockCountCommand(stockCountId), cancellationToken);

    public Task<Guid> ApproveAsync(Guid stockCountId, CancellationToken cancellationToken = default) =>
        sender.Send(new ApproveStockCountCommand(stockCountId), cancellationToken);

    public Task<Guid> PostAsync(Guid stockCountId, CancellationToken cancellationToken = default) =>
        sender.Send(new PostStockCountCommand(stockCountId), cancellationToken);

    public Task<Guid> CancelAsync(Guid stockCountId, CancellationToken cancellationToken = default) =>
        sender.Send(new CancelStockCountCommand(stockCountId), cancellationToken);

    public Task<StockCountDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        sender.Send(new GetStockCountByIdQuery(id), cancellationToken);

    public Task<IReadOnlyList<StockCountLineDto>> GetLinesAsync(Guid stockCountId, CancellationToken cancellationToken = default) =>
        sender.Send(new GetStockCountLinesQuery(stockCountId), cancellationToken);

    public Task<PagedResult<StockCountDto>> GetPageAsync(
        PageRequest request,
        Guid? warehouseId = null,
        DomainStatus? status = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default) =>
        sender.Send(new GetStockCountsQuery(request, warehouseId, status, fromDate, toDate), cancellationToken);
}
