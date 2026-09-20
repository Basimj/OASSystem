using OAS.Application.Inventory.Repositories;
using OAS.Domain.Entities.Inventory;

namespace OAS.Tests.Inventory.Fakes;

public sealed class FakeStockCountRepository
    : FakeGenericRepository<StockCount, Guid>, IStockCountRepository
{
    private readonly List<StockCountLine> _lines;

    public FakeStockCountRepository(
        List<StockCount>? items = null,
        List<StockCountLine>? lines = null)
        : base(items)
    {
        _lines = lines ?? [];
    }

    public List<StockCountLine> Lines => _lines;

    public Task<IReadOnlyList<StockCountLine>> GetLinesAsync(
        Guid stockCountId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<StockCountLine>>(
            _lines.Where(x => x.StockCountId == stockCountId).ToList());
    }

    public Task<StockCountLine?> GetLineAsync(
        Guid stockCountId,
        Guid lineId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _lines.FirstOrDefault(x => x.StockCountId == stockCountId && x.Id == lineId));
    }

    public void AddLine(StockCountLine line) => _lines.Add(line);
}
