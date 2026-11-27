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
}
