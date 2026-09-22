using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public class ReceiptVoucherDomainTests
{
    private Guid _voucherId;
    private Guid _cashAccountId;
    private Guid _customerId;
    private Guid _userId;

    [SetUp]
    public void Setup()
    {
        _voucherId = Guid.NewGuid();
        _cashAccountId = Guid.NewGuid();
        _customerId = Guid.NewGuid();
        _userId = Guid.NewGuid();
    }

    [Test]
    public void Create_ValidReceiptVoucher_InitializesCorrectly()
    {
        var voucher = ReceiptVoucher.Create(
            _voucherId,
            "RV-2026-0001",
            new DateOnly(2026, 1, 15),
            ReceiptPartyType.Customer,
            _customerId,
            "شركة الأمل",
            PaymentMethod.Cash,
            _cashAccountId,
            null,
            5000m,
            ReceiptVoucherStatus.Draft,
            "سند قبض نقدي",
            null);

        Assert.That(voucher.Id, Is.EqualTo(_voucherId));
        Assert.That(voucher.VoucherNumber, Is.EqualTo("RV-2026-0001"));
        Assert.That(voucher.TotalAmount, Is.EqualTo(5000m));
        Assert.That(voucher.Status, Is.EqualTo(ReceiptVoucherStatus.Draft));
        Assert.That(voucher.PartyType, Is.EqualTo(ReceiptPartyType.Customer));
        Assert.That(voucher.PaymentMethod, Is.EqualTo(PaymentMethod.Cash));
    }

    [Test]
    public void Create_ZeroOrNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ReceiptVoucher.Create(
                _voucherId, "RV-2026-0001", new DateOnly(2026, 1, 15),
                ReceiptPartyType.Other, null, "أحمد", PaymentMethod.Cash,
                _cashAccountId, null, 0m, ReceiptVoucherStatus.Draft,
                null, null));
    }

    [Test]
    public void Post_ChangesStatusToPostedAndSetsJournalEntryId()
    {
        var voucher = ReceiptVoucher.Create(
            _voucherId, "RV-2026-0001", new DateOnly(2026, 1, 15),
            ReceiptPartyType.Customer, _customerId, "شركة الأمل", PaymentMethod.Cash,
            _cashAccountId, null, 5000m, ReceiptVoucherStatus.Draft,
            "سند قبض", null);

        var journalId = Guid.NewGuid();
        var postedAt = DateTime.UtcNow;

        voucher.SetJournalEntry(journalId);
        voucher.Approve();
        voucher.Post(_userId, postedAt);

        Assert.That(voucher.Status, Is.EqualTo(ReceiptVoucherStatus.Posted));
        Assert.That(voucher.JournalEntryId, Is.EqualTo(journalId));
        Assert.That(voucher.PostedBy, Is.EqualTo(_userId));
        Assert.That(voucher.PostedAtUtc, Is.EqualTo(postedAt));
    }

    [Test]
    public void Cancel_DraftVoucher_SetsStatusToCancelled()
    {
        var voucher = ReceiptVoucher.Create(
            _voucherId, "RV-2026-0001", new DateOnly(2026, 1, 15),
            ReceiptPartyType.Customer, _customerId, "شركة الأمل", PaymentMethod.Cash,
            _cashAccountId, null, 5000m, ReceiptVoucherStatus.Draft,
            "سند قبض", null);

        voucher.Cancel();

        Assert.That(voucher.Status, Is.EqualTo(ReceiptVoucherStatus.Cancelled));
    }
}
