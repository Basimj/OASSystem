using NUnit.Framework;
using OAS.Contracts.Accounting.CashTransfers;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public sealed class CashBankOperationsTests
{
    [Test]
    public void QuoteUsesDatedBaseRatesInBothDirectionsAndDeductsFeesOnce()
    {
        var yerToUsd = CashTransferPreviewMath.SourceUnitsPerDestination(1m, 550m);
        Assert.That(yerToUsd, Is.EqualTo(550m));
        Assert.That(CashTransferPreviewMath.Received(56000m, 1000m, yerToUsd, 2), Is.EqualTo(100m));
        var usdToYer = CashTransferPreviewMath.SourceUnitsPerDestination(550m, 1m);
        Assert.That(CashTransferPreviewMath.Received(101m, 1m, usdToYer, 0), Is.EqualTo(55000m));
        var cross = CashTransferPreviewMath.SourceUnitsPerDestination(550m, 150m);
        Assert.That(CashTransferPreviewMath.Received(15m, 0m, cross, 2), Is.EqualTo(55m));
    }

    [Test]
    public void QuoteRespectsRoundingAndNeverInventsAMissingRate()
    {
        Assert.That(CashTransferPreviewMath.SourceUnitsPerDestination(null, 1m), Is.Null);
        Assert.That(CashTransferPreviewMath.Received(100m, 0m, null, 2), Is.Zero);
        Assert.That(CashTransferPreviewMath.Received(100m, 100m, 1m, 2), Is.Zero);
        Assert.That(CashTransferPreviewMath.Received(10.005m, 0m, 1m, 2), Is.EqualTo(10.01m));
        Assert.That(CashTransferPreviewMath.Received(100m, 0m, 0m, 2), Is.Zero);
    }

    [Test]
    public void TransferSupportsFeeAndExactReversalOnce()
    {
        var transfer = CashTransfer.Create(Guid.NewGuid(), "TR-TEST");
        var currency = Guid.NewGuid(); var day = new DateOnly(2026, 11, 26);
        transfer.SetDetails(day, Guid.NewGuid(), Guid.NewGuid(), currency, currency, "Cash", "Bank", "YER", "YER",
            100m, 98m, 2m, Guid.NewGuid(), null, "Transfer");
        Assert.Throws<InvalidOperationException>(() => transfer.Post(Guid.NewGuid(), 100m, 98m, 2m, 0m));
        transfer.Approve(Guid.NewGuid(), DateTime.UtcNow);
        transfer.Post(Guid.NewGuid(), 100m, 98m, 2m, 0m);
        Assert.That(transfer.Status, Is.EqualTo(CashTransferStatus.Posted));
        transfer.Reverse(Guid.NewGuid(), day, "Refunded in full");
        Assert.That(transfer.Status, Is.EqualTo(CashTransferStatus.Reversed));
        Assert.Throws<InvalidOperationException>(() => transfer.Reverse(Guid.NewGuid(), day, "Duplicate"));
    }

    [Test]
    public void SameCurrencyTransferRejectsUnbalancedAmount()
    {
        var t = CashTransfer.Create(Guid.NewGuid(), "TR-TEST"); var currency = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => t.SetDetails(new DateOnly(2026, 11, 26), Guid.NewGuid(), Guid.NewGuid(), currency, currency,
            "Cash", "Bank", "YER", "YER", 100m, 99m, 2m, Guid.NewGuid(), null, "Transfer"));
    }

    [Test]
    public void OpeningDifferenceNeedsReasonAndClosingSettlementIsUnique()
    {
        var now = DateTime.UtcNow;
        var shift = CashShift.Create(Guid.NewGuid(), "CS-TEST", Guid.NewGuid(), Guid.NewGuid(), now, 100m, CashShiftStatus.Open);
        Assert.Throws<InvalidOperationException>(() => shift.RecordBookOpeningBalance(105m));
        shift.SetOpeningDifferenceReason("Opening count checked");
        shift.RecordBookOpeningBalance(105m);
        shift.StartClosing(130m);
        shift.Close(125m, Guid.NewGuid(), now.AddMinutes(1));
        shift.Approve();
        var id = Guid.NewGuid(); var account = Guid.NewGuid(); var day = DateOnly.FromDateTime(now);
        shift.RecordSettlement(id, account, day, "Count reviewed");
        Assert.That(shift.SettlementJournalEntryId, Is.EqualTo(id));
        Assert.Throws<InvalidOperationException>(() => shift.RecordSettlement(Guid.NewGuid(), account, day, "Duplicate"));
    }
}
