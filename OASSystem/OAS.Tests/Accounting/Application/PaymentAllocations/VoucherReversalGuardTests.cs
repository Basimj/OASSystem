using NUnit.Framework;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Tests.Accounting.Application.Common;

namespace OAS.Tests.Accounting.Application.PaymentAllocations;

[TestFixture]
public sealed class VoucherReversalGuardTests
{
    [Test]
    public async Task ReversedVoucherCannotBeReusedForAllocation()
    {
        var day = new DateOnly(2026, 11, 26);
        var journal = JournalEntry.Create(Guid.NewGuid(), "JV-TEST", JournalType.Automatic,
            day, day, Guid.NewGuid(), "Test", "Accounting", "ReceiptVoucher", Guid.NewGuid(), JournalEntryStatus.Draft);
        journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, 1, Guid.NewGuid(), 100m, 0m, null, null, null, null, null, null));
        journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, 2, Guid.NewGuid(), 0m, 100m, null, null, null, null, null, null));
        var actor = Guid.NewGuid(); var now = DateTime.UtcNow;
        journal.SetPendingApproval(); journal.Approve(actor, now); journal.Post(actor, now);
        var repo = new FakeRepository<JournalEntry, Guid>(new[] { journal });
        await VoucherReversalGuard.EnsureNotReversedAsync(repo, journal.Id, CancellationToken.None);
        journal.MarkReversed(Guid.NewGuid());
        Assert.ThrowsAsync<ConflictException>(async () => await VoucherReversalGuard.EnsureNotReversedAsync(repo, journal.Id, CancellationToken.None));
        Assert.That(journal.Status, Is.EqualTo(JournalEntryStatus.Posted));
    }

    [Test]
    public void OppositePreservesCustomerCurrencyAndSourceButNotOldShift()
    {
        var customer = Guid.NewGuid(); var currency = Guid.NewGuid(); var source = Guid.NewGuid();
        var day = new DateOnly(2026, 11, 26);
        var line = JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(),
            55000m, 0m, currency, "USD", 2, 100m, 0m, 550m, day,
            ExchangeRateType.Accounting, ExchangeRateSource.System, "Original", customer, null, null,
            "Customer", null, null, null, source);
        line.AssignCashShift(Guid.NewGuid());
        var opposite = line.CreateOpposite(Guid.NewGuid(), Guid.NewGuid());
        Assert.That(opposite.IsExactOppositeOf(line), Is.True);
        Assert.That(opposite.CustomerId, Is.EqualTo(customer));
        Assert.That(opposite.TransactionCreditAmount, Is.EqualTo(100m));
        Assert.That(opposite.CashShiftId, Is.Null);
    }
}
