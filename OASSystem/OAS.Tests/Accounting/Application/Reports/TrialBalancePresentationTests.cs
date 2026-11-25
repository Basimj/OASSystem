using NUnit.Framework;
using OAS.Client.Accounting.Pages;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Tests.Accounting.Application.Reports;

[TestFixture]
public sealed class TrialBalancePresentationTests
{
    private static TrialBalanceRowDto Row(decimal od, decimal oc, decimal pd, decimal pc, decimal priorD, decimal priorC, bool aggregate = false)
    {
        var closing = od - oc + pd - pc;
        return new(Guid.NewGuid(), "100", "حساب", null, AccountClass.Asset, NormalBalance.Debit,
            od, oc, pd, pc, Math.Max(0m, closing), Math.Max(0m, -closing), IsAggregate: aggregate,
            PreviousDebitTotal: priorD, PreviousCreditTotal: priorC);
    }

    [Test]
    public void DirectTotals_DoNotNetDifferentAccounts()
    {
        var first = Row(100, 0, 50, 20, 150, 50);
        var second = Row(0, 100, 20, 50, 50, 150);
        var totals = TrialBalancePresentation.SumDirect([first, second]);
        Assert.Multiple(() =>
        {
            Assert.That(totals.OpeningDebit, Is.EqualTo(100m));
            Assert.That(totals.OpeningCredit, Is.EqualTo(100m));
            Assert.That(totals.PeriodDebit, Is.EqualTo(70m));
            Assert.That(totals.PeriodCredit, Is.EqualTo(70m));
            Assert.That(totals.ClosingDebit, Is.EqualTo(130m));
            Assert.That(totals.ClosingCredit, Is.EqualTo(130m));
            Assert.That(totals.CumulativeDebit, Is.EqualTo(270m));
            Assert.That(totals.CumulativeCredit, Is.EqualTo(270m));
        });
        var header = TrialBalancePresentation.SumDirect([first, second], true);
        Assert.That(header.ClosingDebit, Is.Zero);
        Assert.That(header.ClosingCredit, Is.Zero);
        Assert.That(header.PeriodDebit, Is.EqualTo(70m));
    }

    [Test]
    public void GrossTurnover_PreservesPriorOffsettingMovements()
    {
        var result = TrialBalancePresentation.SumDirect([Row(0, 0, 0, 0, 400, 400)]);
        Assert.That(result.ClosingDebit, Is.Zero);
        Assert.That(result.CumulativeDebit, Is.EqualTo(400m));
        Assert.That(result.CumulativeCredit, Is.EqualTo(400m));
    }

    [Test]
    public void AggregateRows_AreRejectedToPreventDoubleCounting()
    {
        Assert.Throws<ArgumentException>(() => TrialBalancePresentation.SumDirect([Row(100, 0, 0, 0, 100, 0, true)]));
    }

    [Test]
    public void MissingHistoricalTurnover_IsNotReportedAsZero()
    {
        var row = Row(100, 0, 0, 0, 100, 0) with { PreviousDebitTotal = null, PreviousCreditTotal = null };
        var totals = TrialBalancePresentation.SumDirect([row]);
        Assert.That(totals.CumulativeDebit, Is.Null);
        Assert.That(totals.CumulativeCredit, Is.Null);
    }
}
