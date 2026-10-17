using NUnit.Framework;
using OAS.Domain.Sales.Commissions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Tests.Sales.Domain;

[TestFixture]
public sealed class SA05HeaderDiscountAndCommissionTests
{
    [Test]
    public void HeaderDiscountAllocation_IsDeterministic_AndPreservesRoundedTotal()
    {
        var allocations = SalesPricingCalculator.AllocateHeaderDiscount(
            [33.33m, 33.33m, 33.34m], SalesDiscountType.FixedAmount, 10m, 2);

        Assert.Multiple(() =>
        {
            Assert.That(allocations.Sum(), Is.EqualTo(10m));
            Assert.That(allocations, Is.EqualTo(new[] { 3.33m, 3.33m, 3.34m }));
        });
    }

    [Test]
    public void HeaderDiscountAllocation_RejectsPercentageAboveHundred()
    {
        Assert.Throws<OAS.Domain.Exceptions.DomainException>(() =>
            SalesPricingCalculator.AllocateHeaderDiscount([100m], SalesDiscountType.Percentage, 100.01m, 2));
    }

    [Test]
    public void CommissionRule_MatchesAllConfiguredDimensions_AndExposesSpecificity()
    {
        var employeeId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var rule = CommissionRule.Create(Guid.NewGuid(), "R-1", "Specific", employeeId, variantId,
            categoryId, SalesLineType.Frame, 50, CommissionBasisKind.FinalIncludingTax,
            7.5m, new DateOnly(2026, 1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(rule.AppliesTo(employeeId, variantId, categoryId, SalesLineType.Frame, new DateOnly(2026, 10, 17)), Is.True);
            Assert.That(rule.AppliesTo(employeeId, variantId, categoryId, SalesLineType.Lens, new DateOnly(2026, 10, 17)), Is.False);
            Assert.That(rule.Specificity, Is.EqualTo(15));
            Assert.That(rule.Priority, Is.EqualTo(50));
            Assert.That(rule.BasisKind, Is.EqualTo(CommissionBasisKind.FinalIncludingTax));
        });
    }

    [Test]
    public void CommissionStatement_FollowsReviewApproveSettleLifecycle()
    {
        var statement = CommissionStatement.Create(Guid.NewGuid(), "COM-2026-000001", Guid.NewGuid(),
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));
        var ruleId = Guid.NewGuid();
        statement.AddEntry(CommissionEntry.Create(Guid.NewGuid(), statement.Id, statement.EmployeeId,
            "SalesInvoice", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            new DateOnly(2026, 10, 10), 1000m, 10m, ruleId, "R1", "Rule 1",
            CommissionBasisKind.NetAfterDiscountExcludingTax, false));

        var now = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
        statement.MarkCalculated(now, "calculator");
        statement.Review(now.AddMinutes(1), "reviewer");
        statement.Approve(now.AddMinutes(2), "approver");
        statement.Settle(now.AddMinutes(3), "settler");

        Assert.Multiple(() =>
        {
            Assert.That(statement.Status, Is.EqualTo(CommissionStatementStatus.Settled));
            Assert.That(statement.CommissionBaseAmount, Is.EqualTo(100m));
            Assert.That(statement.ReviewedBy, Is.EqualTo("reviewer"));
            Assert.That(statement.ApprovedBy, Is.EqualTo("approver"));
            Assert.That(statement.SettledBy, Is.EqualTo("settler"));
            Assert.That(statement.FinalizedAtUtc, Is.EqualTo(now.AddMinutes(3)));
        });
    }

    [Test]
    public void LateReturnEntry_CanKeepOriginalRuleAndBasisSnapshot()
    {
        var statement = CommissionStatement.Create(Guid.NewGuid(), "COM-2026-000002", Guid.NewGuid(),
            new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));
        var originalRuleId = Guid.NewGuid();
        var returnEntry = CommissionEntry.Create(Guid.NewGuid(), statement.Id, statement.EmployeeId,
            "SalesReturn", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 11, 5), -500m, 8m, originalRuleId, "OLD", "Historical Rule",
            CommissionBasisKind.GrossBeforeDiscount, true);

        statement.AddEntry(returnEntry);

        Assert.Multiple(() =>
        {
            Assert.That(returnEntry.IsReversal, Is.True);
            Assert.That(returnEntry.CommissionRuleId, Is.EqualTo(originalRuleId));
            Assert.That(returnEntry.BasisKindSnapshot, Is.EqualTo(CommissionBasisKind.GrossBeforeDiscount));
            Assert.That(returnEntry.CommissionBaseAmount, Is.EqualTo(-40m));
        });
    }
}
