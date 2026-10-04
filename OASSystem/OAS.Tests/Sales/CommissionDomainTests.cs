using NUnit.Framework;
using OAS.Domain.Sales.Commissions;
using OAS.Domain.Sales.Entities;

namespace OAS.Tests.Sales;

[TestFixture]
public sealed class CommissionDomainTests
{
    [Test]
    public void EmployeeSpecificRule_AppliesOnlyWithinItsEffectiveWindow()
    {
        var employeeId = Guid.NewGuid();
        var rule = CommissionRule.Create(
            Guid.NewGuid(), "COM-5", "Five percent", employeeId, 5m,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        Assert.That(rule.AppliesTo(employeeId, new DateOnly(2026, 6, 1)), Is.True);
        Assert.That(rule.AppliesTo(Guid.NewGuid(), new DateOnly(2026, 6, 1)), Is.False);
        Assert.That(rule.AppliesTo(employeeId, new DateOnly(2027, 1, 1)), Is.False);
    }

    [Test]
    public void CommissionStatement_NetsSalesAndReturns_ThenFinalizes()
    {
        var employeeId = Guid.NewGuid();
        var statement = CommissionStatement.Create(
            Guid.NewGuid(), "COM-2026-000001", employeeId,
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));
        var ruleId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var invoiceLineId = Guid.NewGuid();

        statement.AddEntry(CommissionEntry.Create(
            Guid.NewGuid(), statement.Id, employeeId, "SalesInvoice",
            invoiceId, invoiceLineId, invoiceId, invoiceLineId,
            null, new DateOnly(2026, 10, 2), 1000m, 5m,
            ruleId, "COM-5", "Five percent", false));

        statement.AddEntry(CommissionEntry.Create(
            Guid.NewGuid(), statement.Id, employeeId, "SalesReturn",
            Guid.NewGuid(), Guid.NewGuid(), invoiceId, invoiceLineId,
            Guid.NewGuid(), new DateOnly(2026, 10, 4), -200m, 5m,
            ruleId, "COM-5", "Five percent", true));

        statement.MarkCalculated(DateTimeOffset.UtcNow, "tester");
        statement.Finalize(DateTimeOffset.UtcNow, "manager");

        Assert.That(statement.Status, Is.EqualTo(CommissionStatementStatus.Finalized));
        Assert.That(statement.SalesBaseAmount, Is.EqualTo(1000m));
        Assert.That(statement.ReturnsBaseAmount, Is.EqualTo(200m));
        Assert.That(statement.CommissionBaseAmount, Is.EqualTo(40m));
    }
}
