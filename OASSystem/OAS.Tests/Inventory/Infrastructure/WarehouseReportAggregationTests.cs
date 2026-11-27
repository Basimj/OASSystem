using NUnit.Framework;
using OAS.Contracts.Inventory.Reports;

namespace OAS.Tests.Inventory.Infrastructure;

[TestFixture]
public sealed class WarehouseReportAggregationTests
{
    [Test]
    public void AllWarehousesSumOpeningAndClosingRatherThanLastMovement()
    {
        var a = new WarehouseFlow(10,0,0,0,0,0,0,0,0,10);
        var b = new WarehouseFlow(5,2,0,0,0,0,0,0,0,7);
        var result = WarehouseReportAggregation.SumFlows(new[]{a,b});
        Assert.Multiple(()=> {
            Assert.That(result.Opening, Is.EqualTo(15m));
            Assert.That(result.In, Is.EqualTo(2m));
            Assert.That(result.Closing, Is.EqualTo(17m));
        });
    }

    [Test]
    public void UnknownHistoricalValueIsNotTreatedAsZero()
    {
        var a = new WarehouseFlow(100,0,0,0,0,0,0,0,0,100);
        var b = new WarehouseFlow(null,20,0,0,0,0,0,0,0,null);
        var result = WarehouseReportAggregation.SumFlows(new[]{a,b});
        Assert.Multiple(()=> {
            Assert.That(result.Opening, Is.Null);
            Assert.That(result.Closing, Is.Null);
            Assert.That(result.In, Is.EqualTo(20m));
            Assert.That(WarehouseReportAggregation.SumKnown(new decimal?[]{0, 17}),Is.EqualTo(17m));
            Assert.That(WarehouseReportAggregation.SumKnown(new decimal?[]{0,null}),Is.Null);
        });
    }
    [Test]
    public void ReservationUsesActualWarehouseCostAndAvailableValue()
    {
        var reserved = WarehouseReportAggregation.SumKnown(new[] {
            WarehouseReportAggregation.ReservedValue(0, 100),
            WarehouseReportAggregation.ReservedValue(5, 200) });
        Assert.That(reserved, Is.EqualTo(1000m));
        Assert.That(WarehouseReportAggregation.AvailableValue(3000m, reserved), Is.EqualTo(2000m));
    }

    [Test]
    public void ZeroReservationDoesNotRequireCostButUnknownReservationDoes()
    {
        Assert.That(WarehouseReportAggregation.ReservedValue(0, null), Is.Zero);
        Assert.That(WarehouseReportAggregation.ReservedValue(5, null), Is.Null);
        Assert.That(WarehouseReportAggregation.ReservedValue(null, 100), Is.Null);
        Assert.That(WarehouseReportAggregation.AvailableValue(3000, null), Is.Null);
    }

    [TestCase("reserved", true)]
    [TestCase("partial", true)]
    [TestCase("full", false)]
    [TestCase("available", true)]
    [TestCase("out", false)]
    public void PartiallyReservedStockState(string state, bool expected)
    {
        Assert.That(WarehouseReportAggregation.MatchesStockState(state, 10, 3, true), Is.EqualTo(expected));
    }

    [Test]
    public void HistoricalPositiveStockDoesNotImplyKnownReservation()
    {
        Assert.That(WarehouseReportAggregation.MatchesStockState("available", 10, null, false), Is.True);
        Assert.That(WarehouseReportAggregation.MatchesStockState("reserved", 10, null, false), Is.False);
        Assert.That(WarehouseReportAggregation.MatchesStockState("available", 10, null, true), Is.False);
    }
}
