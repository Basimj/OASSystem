using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public class PaymentVoucherDomainTests
{
    private Guid _voucherId;
    private Guid _bankAccountId;
    private Guid _supplierId;
    private Guid _userId;

    [SetUp]
    public void Setup()
    {
        _voucherId = Guid.NewGuid();
        _bankAccountId = Guid.NewGuid();
        _supplierId = Guid.NewGuid();
        _userId = Guid.NewGuid();
    }

    [Test]
    public void Create_ValidPaymentVoucher_InitializesCorrectly()
    {
        var voucher = PaymentVoucher.Create(
            _voucherId,
            "PV-2026-0001",
            new DateOnly(2026, 1, 15),
            PaymentPartyType.Supplier,
            _supplierId,
            "مؤسسة النور للتوريدات",
            PaymentMethod.BankTransfer,
            null,
            _bankAccountId,
            8500m,
            PaymentVoucherStatus.Draft,
            "سند صرف للمورد",
            null);

        Assert.That(voucher.Id, Is.EqualTo(_voucherId));
        Assert.That(voucher.VoucherNumber, Is.EqualTo("PV-2026-0001"));
        Assert.That(voucher.TotalAmount, Is.EqualTo(8500m));
        Assert.That(voucher.Status, Is.EqualTo(PaymentVoucherStatus.Draft));
        Assert.That(voucher.PartyType, Is.EqualTo(PaymentPartyType.Supplier));
        Assert.That(voucher.PaymentMethod, Is.EqualTo(PaymentMethod.BankTransfer));
    }

    [Test]
    public void Post_ChangesStatusToPostedAndSetsJournalEntryId()
    {
        var voucher = PaymentVoucher.Create(
            _voucherId, "PV-2026-0001", new DateOnly(2026, 1, 15),
            PaymentPartyType.Supplier, _supplierId, "مؤسسة النور", PaymentMethod.BankTransfer,
            null, _bankAccountId, 8500m, PaymentVoucherStatus.Draft,
            "سند صرف", null);

        var journalId = Guid.NewGuid();
        var postedAt = DateTime.UtcNow;

        voucher.SetJournalEntry(journalId);
        voucher.Approve();
        voucher.Post(_userId, postedAt);

        Assert.That(voucher.Status, Is.EqualTo(PaymentVoucherStatus.Posted));
        Assert.That(voucher.JournalEntryId, Is.EqualTo(journalId));
        Assert.That(voucher.PostedBy, Is.EqualTo(_userId));
        Assert.That(voucher.PostedAtUtc, Is.EqualTo(postedAt));
    }

    [Test]
    public void Cancel_DraftPaymentVoucher_SetsStatusToCancelled()
    {
        var voucher = PaymentVoucher.Create(
            _voucherId, "PV-2026-0001", new DateOnly(2026, 1, 15),
            PaymentPartyType.Supplier, _supplierId, "مؤسسة النور", PaymentMethod.BankTransfer,
            null, _bankAccountId, 8500m, PaymentVoucherStatus.Draft,
            "سند صرف", null);

        voucher.Cancel();

        Assert.That(voucher.Status, Is.EqualTo(PaymentVoucherStatus.Cancelled));
    }
}
