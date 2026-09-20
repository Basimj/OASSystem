using NUnit.Framework;
using OAS.Application.Accounting.PaymentVouchers.Commands.CreatePaymentVoucher;
using OAS.Application.Accounting.PaymentVouchers.Commands.SetPaymentVoucherStatus;
using OAS.Application.Accounting.PaymentVouchers.Mapping;
using OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVoucherById;
using OAS.Application.Accounting.ReceiptVouchers.Commands.CreateReceiptVoucher;
using OAS.Application.Accounting.ReceiptVouchers.Commands.SetReceiptVoucherStatus;
using OAS.Application.Accounting.ReceiptVouchers.Mapping;
using OAS.Application.Accounting.ReceiptVouchers.Queries.GetReceiptVoucherById;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;
using DomainReceiptStatus = OAS.Domain.Accounting.Enums.ReceiptVoucherStatus;
using DomainPaymentStatus = OAS.Domain.Accounting.Enums.PaymentVoucherStatus;

namespace OAS.Tests.Accounting.Application.Vouchers;

[TestFixture]
public class VoucherCommandAndQueryTests
{
    private FakeRepository<ReceiptVoucher, Guid> _receiptRepository = null!;
    private FakeRepository<PaymentVoucher, Guid> _paymentRepository = null!;
    private FakeCurrentUser _currentUser = null!;
    private FakeSequenceNumberGenerator _sequenceGenerator = null!;
    private ReceiptVoucherMapper _receiptMapper = null!;
    private PaymentVoucherMapper _paymentMapper = null!;
    private Guid _cashAccountId;
    private Guid _bankAccountId;
    private Guid _revenueAccountId;
    private Guid _expenseAccountId;

    [SetUp]
    public void Setup()
    {
        _receiptRepository = new FakeRepository<ReceiptVoucher, Guid>();
        _paymentRepository = new FakeRepository<PaymentVoucher, Guid>();
        _currentUser = new FakeCurrentUser();
        _sequenceGenerator = new FakeSequenceNumberGenerator();
        _receiptMapper = new ReceiptVoucherMapper();
        _paymentMapper = new PaymentVoucherMapper();
        _cashAccountId = Guid.NewGuid();
        _bankAccountId = Guid.NewGuid();
        _revenueAccountId = Guid.NewGuid();
        _expenseAccountId = Guid.NewGuid();
    }

    [Test]
    public async Task CreateReceiptVoucherCommandHandler_CreatesVoucherWithLines()
    {
        var handler = new CreateReceiptVoucherCommandHandler(
            _receiptRepository,
            _sequenceGenerator,
            _currentUser,
            TimeProvider.System);

        var request = new CreateReceiptVoucherRequest(
            VoucherDate: new DateOnly(2026, 1, 15),
            PartyType: ReceiptPartyType.Customer,
            CustomerId: Guid.NewGuid(),
            ReceivedFrom: "عميل تجريبي",
            PaymentMethod: PaymentMethod.Cash,
            CashAccountId: _cashAccountId,
            BankAccountId: null,
            TotalAmount: 3500m,
            Description: "سند قبض نقدي",
            Lines:
            [
                new CreateReceiptVoucherLineRequest(
                    AccountId: _revenueAccountId,
                    Amount: 3500m,
                    ReferenceType: null,
                    ReferenceId: null,
                    Description: "إيراد مبيعات")
            ]);

        var command = new CreateReceiptVoucherCommand(request);
        var voucherId = await handler.Handle(command, CancellationToken.None);

        Assert.That(voucherId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(_receiptRepository.Items.Count, Is.EqualTo(1));
        var saved = _receiptRepository.Items[0];
        Assert.That(saved.VoucherNumber, Does.StartWith("RV-2026-"));
        Assert.That(saved.TotalAmount, Is.EqualTo(3500m));
        Assert.That(saved.Lines.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task CreatePaymentVoucherCommandHandler_CreatesPaymentVoucherWithLines()
    {
        var handler = new CreatePaymentVoucherCommandHandler(
            _paymentRepository,
            _sequenceGenerator,
            _currentUser,
            TimeProvider.System);

        var request = new CreatePaymentVoucherRequest(
            VoucherDate: new DateOnly(2026, 1, 15),
            PartyType: PaymentPartyType.Supplier,
            SupplierId: Guid.NewGuid(),
            BeneficiaryName: "مورد تجريبي",
            PaymentMethod: PaymentMethod.BankTransfer,
            CashAccountId: null,
            BankAccountId: _bankAccountId,
            TotalAmount: 4200m,
            Description: "سند صرف بنكي",
            Lines:
            [
                new CreatePaymentVoucherLineRequest(
                    AccountId: _expenseAccountId,
                    Amount: 4200m,
                    ReferenceType: null,
                    ReferenceId: null,
                    Description: "دفعة من الحساب")
            ]);

        var command = new CreatePaymentVoucherCommand(request);
        var voucherId = await handler.Handle(command, CancellationToken.None);

        Assert.That(voucherId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(_paymentRepository.Items.Count, Is.EqualTo(1));
        var saved = _paymentRepository.Items[0];
        Assert.That(saved.VoucherNumber, Does.StartWith("PV-2026-"));
        Assert.That(saved.TotalAmount, Is.EqualTo(4200m));
    }

    [Test]
    public async Task GetReceiptVoucherByIdQueryHandler_ReturnsDto()
    {
        var voucher = ReceiptVoucher.Create(
            Guid.NewGuid(), "RV-2026-000001", new DateOnly(2026, 1, 15),
            OAS.Domain.Accounting.Enums.ReceiptPartyType.Customer, Guid.NewGuid(), "عميل",
            OAS.Domain.Accounting.Enums.PaymentMethod.Cash, _cashAccountId, null, 1500m,
            DomainReceiptStatus.Draft, "سند", null, Guid.Parse(_currentUser.UserId!), DateTime.UtcNow);

        await _receiptRepository.AddAsync(voucher);

        var handler = new GetReceiptVoucherByIdQueryHandler(_receiptRepository, _receiptMapper);
        var query = new GetReceiptVoucherByIdQuery(voucher.Id);
        var dto = await handler.Handle(query, CancellationToken.None);

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.VoucherNumber, Is.EqualTo("RV-2026-000001"));
        Assert.That(dto.TotalAmount, Is.EqualTo(1500m));
    }
}
