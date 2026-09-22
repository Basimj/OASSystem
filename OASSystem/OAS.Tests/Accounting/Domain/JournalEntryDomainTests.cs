using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public class JournalEntryDomainTests
{
    private Guid _journalId;
    private Guid _periodId;
    private Guid _userId;
    private Guid _accountId1;
    private Guid _accountId2;

    [SetUp]
    public void Setup()
    {
        _journalId = Guid.NewGuid();
        _periodId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        _accountId1 = Guid.NewGuid();
        _accountId2 = Guid.NewGuid();
    }

    [Test]
    public void Create_ValidParameters_CreatesDraftJournal()
    {
        var entry = JournalEntry.Create(
            _journalId,
            "JV-2026-0001",
            JournalType.Manual,
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 1, 15),
            _periodId,
            "قيد افتتاحي",
            sourceModule: null,
            sourceDocumentType: null,
            sourceDocumentId: null,
            status: JournalEntryStatus.Draft);

        Assert.That(entry.Id, Is.EqualTo(_journalId));
        Assert.That(entry.JournalNumber, Is.EqualTo("JV-2026-0001"));
        Assert.That(entry.Status, Is.EqualTo(JournalEntryStatus.Draft));
        Assert.That(entry.Lines, Is.Empty);
    }

    [Test]
    public void AddLine_ValidBalancedLines_AddsSuccessfully()
    {
        var entry = CreateSampleDraftJournal();

        var line1 = JournalEntryLine.Create(
            Guid.NewGuid(), _journalId, 1, _accountId1, 1000m, 0m, "Debit line",
            null, null, null, null, null);

        var line2 = JournalEntryLine.Create(
            Guid.NewGuid(), _journalId, 2, _accountId2, 0m, 1000m, "Credit line",
            null, null, null, null, null);

        entry.AddLine(line1);
        entry.AddLine(line2);

        Assert.That(entry.Lines.Count, Is.EqualTo(2));
        Assert.That(entry.Lines.Sum(x => x.DebitAmount), Is.EqualTo(1000m));
        Assert.That(entry.Lines.Sum(x => x.CreditAmount), Is.EqualTo(1000m));
    }

    [Test]
    public void AddLine_LineBelongingToDifferentJournal_ThrowsInvalidOperationException()
    {
        var entry = CreateSampleDraftJournal();
        var otherJournalId = Guid.NewGuid();

        var line = JournalEntryLine.Create(
            Guid.NewGuid(), otherJournalId, 1, _accountId1, 500m, 0m, "Invalid line",
            null, null, null, null, null);

        Assert.Throws<InvalidOperationException>(() => entry.AddLine(line));
    }

    [Test]
    public void Post_UnbalancedLines_ThrowsInvalidOperationException()
    {
        var entry = CreateSampleDraftJournal();

        var line1 = JournalEntryLine.Create(
            Guid.NewGuid(), _journalId, 1, _accountId1, 1000m, 0m, "Debit line",
            null, null, null, null, null);

        var line2 = JournalEntryLine.Create(
            Guid.NewGuid(), _journalId, 2, _accountId2, 0m, 500m, "Credit line",
            null, null, null, null, null);

        entry.AddLine(line1);
        entry.AddLine(line2);

        Assert.Throws<InvalidOperationException>(() =>
            entry.SetPendingApproval());
    }

    [Test]
    public void Post_BalancedLines_ChangesStatusToPosted()
    {
        var entry = CreateSampleDraftJournal();

        var line1 = JournalEntryLine.Create(
            Guid.NewGuid(), _journalId, 1, _accountId1, 1500m, 0m, "Debit line",
            null, null, null, null, null);

        var line2 = JournalEntryLine.Create(
            Guid.NewGuid(), _journalId, 2, _accountId2, 0m, 1500m, "Credit line",
            null, null, null, null, null);

        entry.AddLine(line1);
        entry.AddLine(line2);

        var postedAt = DateTime.UtcNow;
        entry.SetPendingApproval();
        entry.Approve(_userId, postedAt);
        entry.Post(_userId, postedAt);

        Assert.That(entry.Status, Is.EqualTo(JournalEntryStatus.Posted));
        Assert.That(entry.PostedBy, Is.EqualTo(_userId));
        Assert.That(entry.PostedAtUtc, Is.EqualTo(postedAt));
    }

    [Test]
    public void Reverse_PostedJournal_SetsStatusToReversed()
    {
        var entry = CreateSampleDraftJournal();

        var line1 = JournalEntryLine.Create(
            Guid.NewGuid(), _journalId, 1, _accountId1, 100m, 0m, "Debit",
            null, null, null, null, null);
        var line2 = JournalEntryLine.Create(
            Guid.NewGuid(), _journalId, 2, _accountId2, 0m, 100m, "Credit",
            null, null, null, null, null);

        entry.AddLine(line1);
        entry.AddLine(line2);
        entry.SetPendingApproval();
        entry.Approve(_userId, DateTime.UtcNow);
        entry.Post(_userId, DateTime.UtcNow);

        var reversalId = Guid.NewGuid();
        entry.MarkReversed(reversalId);

        Assert.That(entry.Status, Is.EqualTo(JournalEntryStatus.Reversed));
        Assert.That(entry.ReversedJournalId, Is.EqualTo(reversalId));
    }

    [Test]
    public void AddLine_OnPostedJournal_ThrowsInvalidOperationException()
    {
        var entry = CreateSampleDraftJournal();

        var line1 = JournalEntryLine.Create(
            Guid.NewGuid(), _journalId, 1, _accountId1, 100m, 0m, "Debit",
            null, null, null, null, null);
        var line2 = JournalEntryLine.Create(
            Guid.NewGuid(), _journalId, 2, _accountId2, 0m, 100m, "Credit",
            null, null, null, null, null);

        entry.AddLine(line1);
        entry.AddLine(line2);
        entry.SetPendingApproval();
        entry.Approve(_userId, DateTime.UtcNow);
        entry.Post(_userId, DateTime.UtcNow);

        var newLine = JournalEntryLine.Create(
            Guid.NewGuid(), _journalId, 3, _accountId1, 50m, 0m, "Extra line",
            null, null, null, null, null);

        Assert.Throws<InvalidOperationException>(() => entry.AddLine(newLine));
    }

    private JournalEntry CreateSampleDraftJournal()
    {
        return JournalEntry.Create(
            _journalId,
            "JV-2026-0001",
            JournalType.Manual,
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 1, 15),
            _periodId,
            "قيد تجريبي",
            null, null, null,
            JournalEntryStatus.Draft);
    }
}
