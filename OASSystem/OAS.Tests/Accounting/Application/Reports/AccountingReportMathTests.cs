using NUnit.Framework;
using OAS.Application.Accounting.Reports.Services;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Application.Reports;

[TestFixture]
public sealed class AccountingReportMathTests
{
    [Test]
    public void DebitNormal_UsesDebitMinusCredit()
    {
        Assert.That(AccountingReportMath.NaturalDelta(NormalBalance.Debit, 150m, 40m), Is.EqualTo(110m));
    }

    [Test]
    public void CreditNormal_UsesCreditMinusDebit()
    {
        Assert.That(AccountingReportMath.NaturalDelta(NormalBalance.Credit, 40m, 150m), Is.EqualTo(110m));
    }

    [TestCase(125, 125, true)]
    [TestCase(125, 124.99, false)]
    public void BalanceCheck_IsExact(decimal debit, decimal credit, bool expected)
    {
        Assert.That(AccountingReportMath.IsBalanced(debit, credit), Is.EqualTo(expected));
    }

    [TestCase(50, 50, 0)]
    [TestCase(-50, 0, 50)]
    [TestCase(0, 0, 0)]
    public void SplitNet_ProducesTrialBalanceColumns(decimal net, decimal expectedDebit, decimal expectedCredit)
    {
        var result = AccountingReportMath.SplitNet(net);
        Assert.Multiple(() =>
        {
            Assert.That(result.Debit, Is.EqualTo(expectedDebit));
            Assert.That(result.Credit, Is.EqualTo(expectedCredit));
        });
    }
}
