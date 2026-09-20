using NUnit.Framework;
using OAS.Domain.Entities.Inventory;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class MovingAverageCostingTests
{
    [Test]
    public void InitialInbound_SetsAverageCostAndInventoryValue()
    {
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var balance = new InventoryBalance(warehouseId, variantId, 0, 0, 0, 0, 0);
        var now = DateTimeOffset.UtcNow;

        balance.ApplyInbound(10, 25.50m, now);

        Assert.That(balance.OnHandQuantity, Is.EqualTo(10));
        Assert.That(balance.AverageUnitCost, Is.EqualTo(25.50m));
        Assert.That(balance.InventoryValue, Is.EqualTo(255.00m));
        Assert.That(balance.LastMovementAtUtc, Is.EqualTo(now));
    }

    [Test]
    public void SubsequentInbound_CalculatesMovingAverageCorrectly()
    {
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var balance = new InventoryBalance(warehouseId, variantId, 0, 0, 0, 0, 0);
        var now = DateTimeOffset.UtcNow;

        // First receipt: 10 units @ $10.00 = $100.00
        balance.ApplyInbound(10, 10.00m, now);
        Assert.That(balance.OnHandQuantity, Is.EqualTo(10));
        Assert.That(balance.AverageUnitCost, Is.EqualTo(10.00m));

        // Second receipt: 10 units @ $20.00 = $200.00
        // New total: 20 units, Total value = $300.00 -> New average cost = $15.00
        balance.ApplyInbound(10, 20.00m, now);
        Assert.That(balance.OnHandQuantity, Is.EqualTo(20));
        Assert.That(balance.AverageUnitCost, Is.EqualTo(15.00m));
        Assert.That(balance.InventoryValue, Is.EqualTo(300.00m));
    }

    [Test]
    public void Outbound_PreservesMovingAverageCost()
    {
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var balance = new InventoryBalance(warehouseId, variantId, 0, 0, 0, 0, 0);
        var now = DateTimeOffset.UtcNow;

        balance.ApplyInbound(20, 15.00m, now);
        Assert.That(balance.AverageUnitCost, Is.EqualTo(15.00m));

        // Issue 5 units: average cost remains $15.00, remaining on hand = 15, inventory value = $225.00
        balance.ApplyOutbound(5, now);
        Assert.That(balance.OnHandQuantity, Is.EqualTo(15));
        Assert.That(balance.AverageUnitCost, Is.EqualTo(15.00m));
        Assert.That(balance.InventoryValue, Is.EqualTo(225.00m));
    }

    [Test]
    public void StockCountAdjustment_Positive_CalculatesMovingAverage()
    {
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var balance = new InventoryBalance(warehouseId, variantId, 0, 0, 0, 0, 0);
        var now = DateTimeOffset.UtcNow;

        balance.ApplyInbound(10, 10.00m, now);

        // Adjust +2 units at snapshot unit cost $10.00
        balance.ApplyAdjustment(2, 10.00m, now);
        Assert.That(balance.OnHandQuantity, Is.EqualTo(12));
        Assert.That(balance.AverageUnitCost, Is.EqualTo(10.00m));
        Assert.That(balance.InventoryValue, Is.EqualTo(120.00m));
    }

    [Test]
    public void StockCountAdjustment_Negative_ReducesQuantityPreservingCost()
    {
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var balance = new InventoryBalance(warehouseId, variantId, 0, 0, 0, 0, 0);
        var now = DateTimeOffset.UtcNow;

        balance.ApplyInbound(10, 10.00m, now);

        // Adjust -3 units
        balance.ApplyAdjustment(-3, 10.00m, now);
        Assert.That(balance.OnHandQuantity, Is.EqualTo(7));
        Assert.That(balance.AverageUnitCost, Is.EqualTo(10.00m));
        Assert.That(balance.InventoryValue, Is.EqualTo(70.00m));
    }

    [Test]
    public void InvalidQuantity_ThrowsException()
    {
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var balance = new InventoryBalance(warehouseId, variantId, 0, 0, 0, 0, 0);
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentOutOfRangeException>(() => balance.ApplyInbound(0, 10m, now));
        Assert.Throws<ArgumentOutOfRangeException>(() => balance.ApplyInbound(-5, 10m, now));
        Assert.Throws<ArgumentOutOfRangeException>(() => balance.ApplyOutbound(0, now));
        Assert.Throws<ArgumentOutOfRangeException>(() => balance.ApplyOutbound(-5, now));
    }
}
