using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Exceptions;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public class BankReconciliationDomainTests
{
    private Guid _bankAccountId;
    private Guid _currencyId;
    private Guid _userId;

    [SetUp]
    public void Setup()
    {
        _bankAccountId = Guid.NewGuid();
        _currencyId = Guid.NewGuid();
        _userId = Guid.NewGuid();
    }

    [Test]
    public void CreateManualStatement_ValidData_CreatesDraftStatement()
    {
        var statement = BankStatement.CreateManual(
            Guid.NewGuid(),
            _bankAccountId,
            "BS-001",
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 31),
            1000m,
            1500m,
            _currencyId);

        Assert.That(statement.Status, Is.EqualTo(BankStatementStatus.Draft));
        Assert.That(statement.SourceType, Is.EqualTo(BankStatementSourceType.Manual));
        Assert.That(statement.BankAccountId, Is.EqualTo(_bankAccountId));
    }

    [Test]
    public void CreateStatement_StartDateAfterEndDate_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => BankStatement.CreateManual(
            Guid.NewGuid(),
            _bankAccountId,
            null,
            new DateOnly(2026, 10, 31),
            new DateOnly(2026, 10, 1),
            0m,
            0m,
            _currencyId));
    }

    [Test]
    public void StatementLine_BothDebitAndCreditPositive_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => BankStatementLine.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 10, 5),
            null,
            null,
            null,
            100m,
            100m,
            null,
            null));
    }

    [Test]
    public void StatementLine_RecalculateMatchStatus_TransitionsCorrectly()
    {
        var line = CreateCreditLine(100m);

        line.RecalculateMatchStatus(0m);
        Assert.That(line.MatchStatus, Is.EqualTo(BankStatementLineMatchStatus.Unmatched));

        line.RecalculateMatchStatus(40m);
        Assert.That(line.MatchStatus, Is.EqualTo(BankStatementLineMatchStatus.PartiallyMatched));

        line.RecalculateMatchStatus(100m);
        Assert.That(line.MatchStatus, Is.EqualTo(BankStatementLineMatchStatus.Matched));
    }

    [Test]
    public void StatementLine_IgnoreMatchedLine_ThrowsDomainException()
    {
        var line = CreateCreditLine(100m);
        line.RecalculateMatchStatus(50m);

        Assert.Throws<DomainException>(() =>
            line.Ignore("Bank-only memo", _userId, DateTimeOffset.UtcNow));
    }

    [Test]
    public void Match_ExactlyOneSourceRequired()
    {
        Assert.Throws<DomainException>(() => BankReconciliationMatch.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            journalEntryLineId: Guid.NewGuid(),
            receiptVoucherLineId: Guid.NewGuid(),
            paymentVoucherLineId: null,
            matchedAmount: 100m,
            matchType: BankMatchType.JournalEntry,
            matchMethod: BankMatchMethod.Manual));
    }

    [Test]
    public void Match_SourceMustAgreeWithMatchType()
    {
        Assert.Throws<DomainException>(() => BankReconciliationMatch.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            journalEntryLineId: Guid.NewGuid(),
            receiptVoucherLineId: null,
            paymentVoucherLineId: null,
            matchedAmount: 100m,
            matchType: BankMatchType.PaymentVoucher,
            matchMethod: BankMatchMethod.Manual));
    }

    [Test]
    public void Reconciliation_CloseWithDifferenceWithoutPolicy_ThrowsDomainException()
    {
        var reconciliation = BankReconciliation.Create(
            Guid.NewGuid(),
            _bankAccountId,
            Guid.NewGuid(),
            new DateOnly(2026, 10, 31),
            1000m);

        reconciliation.Submit(_userId, DateTimeOffset.UtcNow);
        reconciliation.Approve(_userId, DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => reconciliation.Close(
            bookClosingBalance: 990m,
            statementClosingBalance: 1000m,
            unresolvedDifferenceAllowed: false,
            closedBy: _userId,
            closedAtUtc: DateTimeOffset.UtcNow));
    }

    [Test]
    public void Reconciliation_CloseBalanced_SnapshotsBalancesAndCloses()
    {
        var reconciliation = BankReconciliation.Create(
            Guid.NewGuid(),
            _bankAccountId,
            Guid.NewGuid(),
            new DateOnly(2026, 10, 31),
            1000m);

        reconciliation.Submit(_userId, DateTimeOffset.UtcNow);
        reconciliation.Approve(_userId, DateTimeOffset.UtcNow);
        reconciliation.Close(1000m, 1000m, false, _userId, DateTimeOffset.UtcNow);

        Assert.That(reconciliation.Status, Is.EqualTo(BankReconciliationStatus.Reconciled));
        Assert.That(reconciliation.Difference, Is.Zero);
        Assert.That(reconciliation.BookClosingBalance, Is.EqualTo(1000m));
    }

    [Test]
    public void Reconciliation_ReopenRequiresReason()
    {
        var reconciliation = BankReconciliation.Create(
            Guid.NewGuid(),
            _bankAccountId,
            Guid.NewGuid(),
            new DateOnly(2026, 10, 31),
            1000m);

        reconciliation.Submit(_userId, DateTimeOffset.UtcNow);
        reconciliation.Approve(_userId, DateTimeOffset.UtcNow);
        reconciliation.Close(1000m, 1000m, false, _userId, DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() =>
            reconciliation.Reopen(_userId, DateTimeOffset.UtcNow, " "));
    }

    private static BankStatementLine CreateCreditLine(decimal amount) =>
        BankStatementLine.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 10, 5),
            null,
            "REF-1",
            "Test line",
            0m,
            amount,
            null,
            "EXT-1");
}
