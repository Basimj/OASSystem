using OAS.Application.Inventory.Repositories;
using OAS.Domain.Entities.Inventory;

namespace OAS.Tests.Inventory.Fakes;

public sealed class FakeInventoryTransactionRepository
    : FakeGenericRepository<InventoryTransaction, Guid>, IInventoryTransactionRepository
{
    private readonly List<InventoryTransactionLine> _lines;

    public FakeInventoryTransactionRepository(
        List<InventoryTransaction>? items = null,
        List<InventoryTransactionLine>? lines = null)
        : base(items)
    {
        _lines = lines ?? [];
    }

    public List<InventoryTransactionLine> Lines => _lines;

    public Task<IReadOnlyList<InventoryTransactionLine>> GetLinesAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<InventoryTransactionLine>>(
            _lines.Where(x => x.TransactionId == transactionId).ToList());
    }

    public void AddLine(InventoryTransactionLine line) => _lines.Add(line);
}
