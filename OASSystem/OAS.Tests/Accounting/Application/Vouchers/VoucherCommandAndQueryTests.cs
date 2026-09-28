using NUnit.Framework;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.PaymentVouchers.Commands.CreatePaymentVoucher;
using OAS.Application.Accounting.PaymentVouchers.Commands.SetPaymentVoucherStatus;
using OAS.Application.Accounting.PaymentVouchers.Commands.UpdatePaymentVoucher;
using OAS.Application.Accounting.PaymentVouchers.Mapping;
using OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVoucherById;
using OAS.Application.Accounting.ReceiptVouchers.Commands.CreateReceiptVoucher;
using OAS.Application.Accounting.ReceiptVouchers.Commands.SetReceiptVoucherStatus;
using OAS.Application.Accounting.ReceiptVouchers.Commands.UpdateReceiptVoucher;
using OAS.Application.Accounting.ReceiptVouchers.Mapping;
using OAS.Application.Accounting.ReceiptVouchers.Queries.GetReceiptVoucherById;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;
using DomainExchangeRateSource = OAS.Domain.Accounting.Enums.ExchangeRateSource;
using DomainExchangeRateType = OAS.Domain.Accounting.Enums.ExchangeRateType;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;
using DomainPartyType = OAS.Domain.Accounting.Enums.SettlementPartyType;
using DomainReceiptVoucherStatus = OAS.Domain.Accounting.Enums.ReceiptVoucherStatus;
using DomainPaymentVoucherStatus = OAS.Domain.Accounting.Enums.PaymentVoucherStatus;

namespace OAS.Tests.Accounting.Application.Vouchers;

[TestFixture]
public sealed class VoucherCommandAndQueryTests
{
    private FakeRepository<ReceiptVoucher, Guid> _receiptRepository = null!;
    private FakeRepository<ReceiptVoucherLine, Guid> _receiptLineRepository = null!;
    private FakeRepository<PaymentVoucher, Guid> _paymentRepository = null!;
    private FakeRepository<PaymentVoucherLine, Guid> _paymentLineRepository = null!;
    private FakeRepository<AccountingSettings, Guid> _settingsRepository = null!;
    private FakeRepository<Currency, Guid> _currencyRepository = null!;
    private FakeSequenceNumberGenerator _sequence = null!;
    private Guid _currencyId;
    private Guid _counterpartyAccountId;
    private Guid _settlementAccountId;

    [SetUp]
    public async Task Setup()
    {
        _receiptRepository = new();
        _receiptLineRepository = new();
        _paymentRepository = new();
        _paymentLineRepository = new();
        _settingsRepository = new();
        _currencyRepository = new();
        _sequence = new();
        _currencyId = Guid.NewGuid();
        _counterpartyAccountId = Guid.NewGuid();
        _settlementAccountId = Guid.NewGuid();
        await _currencyRepository.AddAsync(Currency.Create(_currencyId, "YER", "الريال اليمني", null, "ر.ي", 2, true));
        await _settingsRepository.AddAsync(AccountingSettings.Create(_currencyId));
    }

    [Test]
    public async Task CreateReceiptVoucherCommandHandler_CreatesSettlementVoucher()
    {
        var resolver = new FakeSettlementResolver(_counterpartyAccountId, _settlementAccountId, _currencyId, 1m);
        var handler = new CreateReceiptVoucherCommandHandler(_receiptRepository, _sequence, resolver, _settingsRepository, _currencyRepository);
        var request = new CreateReceiptVoucherRequest(
            new DateOnly(2026, 1, 15),
            "سند قبض",
            [new CreateReceiptVoucherLineRequest(SettlementPartyType.Other, null, null, null, "عميل نقدي", _counterpartyAccountId, PaymentMethod.Cash, Guid.NewGuid(), null, null, _currencyId, 3500m)]);

        var id = await handler.Handle(new CreateReceiptVoucherCommand(request), CancellationToken.None);
        var saved = _receiptRepository.Items.Single(x => x.Id == id);

        Assert.Multiple(() =>
        {
            Assert.That(saved.VoucherNumber, Does.StartWith("RV-2026-"));
            Assert.That(saved.BaseCurrencyId, Is.EqualTo(_currencyId));
            Assert.That(saved.BaseTotalAmount, Is.EqualTo(3500m));
            Assert.That(saved.Lines, Has.Count.EqualTo(1));
            Assert.That(saved.Lines.Single().CounterpartyAccountId, Is.EqualTo(_counterpartyAccountId));
            Assert.That(saved.Lines.Single().SettlementAccountId, Is.EqualTo(_settlementAccountId));
        });
    }

    [Test]
    public async Task CreatePaymentVoucherCommandHandler_CreatesSettlementVoucher()
    {
        var resolver = new FakeSettlementResolver(_counterpartyAccountId, _settlementAccountId, _currencyId, 1m);
        var handler = new CreatePaymentVoucherCommandHandler(_paymentRepository, _sequence, resolver, _settingsRepository, _currencyRepository);
        var request = new CreatePaymentVoucherRequest(
            new DateOnly(2026, 1, 15),
            "سند صرف",
            [new CreatePaymentVoucherLineRequest(SettlementPartyType.Other, null, null, null, "مورد نقدي", _counterpartyAccountId, PaymentMethod.BankTransfer, null, Guid.NewGuid(), null, _currencyId, 4200m)]);

        var id = await handler.Handle(new CreatePaymentVoucherCommand(request), CancellationToken.None);
        var saved = _paymentRepository.Items.Single(x => x.Id == id);

        Assert.Multiple(() =>
        {
            Assert.That(saved.VoucherNumber, Does.StartWith("PV-2026-"));
            Assert.That(saved.BaseCurrencyId, Is.EqualTo(_currencyId));
            Assert.That(saved.BaseTotalAmount, Is.EqualTo(4200m));
            Assert.That(saved.Lines, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task UpdateReceiptVoucherCommandHandler_SaveWithoutChangingCashSelection_Succeeds()
    {
        var cashAccountId = Guid.NewGuid();
        var resolver = new FakeSettlementResolver(_counterpartyAccountId, _settlementAccountId, _currencyId, 1m);
        var voucher = ReceiptVoucher.CreateSettlementDocument(
            Guid.NewGuid(), "RV-2026-EDIT", new DateOnly(2026, 1, 15),
            _currencyId, "YER", 2, 3500m, "سند قبض");
        var existingLine = ReceiptVoucherLine.CreateSettlement(
            Guid.NewGuid(), voucher.Id, 1, DomainPartyType.Other, null, null, null,
            "عميل نقدي", _counterpartyAccountId, DomainPaymentMethod.Cash, cashAccountId, null,
            _settlementAccountId, _currencyId, "YER", "ر.ي", 2, 3500m, 1m, voucher.VoucherDate,
            DomainExchangeRateType.Accounting, DomainExchangeRateSource.System, 3500m,
            null, null, null, null, "سطر");

        await _receiptRepository.AddAsync(voucher);
        await _receiptLineRepository.AddAsync(existingLine);

        var handler = new UpdateReceiptVoucherCommandHandler(
            _receiptRepository, _receiptLineRepository, resolver, _settingsRepository, _currencyRepository);

        var request = new UpdateReceiptVoucherRequest(
            voucher.VoucherDate,
            voucher.Description,
            [new CreateReceiptVoucherLineRequest(
                SettlementPartyType.Other, null, null, null, "عميل نقدي", _counterpartyAccountId,
                PaymentMethod.Cash, cashAccountId, null, null, _currencyId, 3500m)],
            Convert.ToBase64String(voucher.RowVersion));

        await handler.Handle(new UpdateReceiptVoucherCommand(voucher.Id, request), CancellationToken.None);

        var updatedLine = _receiptLineRepository.Items.Single();
        Assert.Multiple(() =>
        {
            Assert.That(voucher.Status, Is.EqualTo(DomainReceiptVoucherStatus.Draft));
            Assert.That(updatedLine.CashAccountId, Is.EqualTo(cashAccountId));
            Assert.That(updatedLine.BankAccountId, Is.Null);
            Assert.That(updatedLine.SettlementAccountId, Is.EqualTo(_settlementAccountId));
        });
    }

    [Test]
    public async Task UpdatePaymentVoucherCommandHandler_SaveWithoutChangingBankSelection_Succeeds()
    {
        var bankAccountId = Guid.NewGuid();
        var resolver = new FakeSettlementResolver(_counterpartyAccountId, _settlementAccountId, _currencyId, 1m);
        var voucher = PaymentVoucher.CreateSettlementDocument(
            Guid.NewGuid(), "PV-2026-EDIT", new DateOnly(2026, 1, 15),
            _currencyId, "YER", 2, 4200m, "سند صرف");
        var existingLine = PaymentVoucherLine.CreateSettlement(
            Guid.NewGuid(), voucher.Id, 1, DomainPartyType.Other, null, null, null,
            "مورد", _counterpartyAccountId, DomainPaymentMethod.BankTransfer, null, bankAccountId,
            _settlementAccountId, _currencyId, "YER", "ر.ي", 2, 4200m, 1m, voucher.VoucherDate,
            DomainExchangeRateType.Accounting, DomainExchangeRateSource.System, 4200m,
            null, null, null, null, "سطر");

        await _paymentRepository.AddAsync(voucher);
        await _paymentLineRepository.AddAsync(existingLine);

        var handler = new UpdatePaymentVoucherCommandHandler(
            _paymentRepository, _paymentLineRepository, resolver, _settingsRepository, _currencyRepository);

        var request = new UpdatePaymentVoucherRequest(
            voucher.VoucherDate,
            voucher.Description,
            [new CreatePaymentVoucherLineRequest(
                SettlementPartyType.Other, null, null, null, "مورد", _counterpartyAccountId,
                PaymentMethod.BankTransfer, null, bankAccountId, null, _currencyId, 4200m)],
            Convert.ToBase64String(voucher.RowVersion));

        await handler.Handle(new UpdatePaymentVoucherCommand(voucher.Id, request), CancellationToken.None);

        var updatedLine = _paymentLineRepository.Items.Single();
        Assert.Multiple(() =>
        {
            Assert.That(voucher.Status, Is.EqualTo(DomainPaymentVoucherStatus.Draft));
            Assert.That(updatedLine.CashAccountId, Is.Null);
            Assert.That(updatedLine.BankAccountId, Is.EqualTo(bankAccountId));
            Assert.That(updatedLine.SettlementAccountId, Is.EqualTo(_settlementAccountId));
        });
    }

    [Test]
    public async Task ReceiptVoucherStatusHandler_ApprovesThenPostsAndAssignsJournal()
    {
        var userId = Guid.NewGuid();
        var voucher = ReceiptVoucher.Create(
            Guid.NewGuid(), "RV-2026-STATUS", new DateOnly(2026, 2, 1),
            OAS.Domain.Accounting.Enums.ReceiptPartyType.Other, null, "طرف", DomainPaymentMethod.Other,
            null, null, 100m, DomainReceiptVoucherStatus.Draft, "اختبار دورة الحالة", null);
        var line = ReceiptVoucherLine.Create(
            Guid.NewGuid(), voucher.Id, 1, _counterpartyAccountId, 100m, null, null, "سطر");

        await _receiptRepository.AddAsync(voucher);
        await _receiptLineRepository.AddAsync(line);

        var posting = new FakeAccountingDocumentPostingService();
        var handler = new SetReceiptVoucherStatusCommandHandler(
            _receiptRepository, _receiptLineRepository, posting, new FakePermissionChecker(),
            new FakeCurrentUser { UserId = userId.ToString() }, TimeProvider.System);

        var rowVersion = Convert.ToBase64String(voucher.RowVersion);
        await handler.Handle(
            new SetReceiptVoucherStatusCommand(
                voucher.Id,
                new SetReceiptVoucherStatusRequest(ReceiptVoucherStatus.Approved, rowVersion)),
            CancellationToken.None);

        Assert.That(voucher.Status, Is.EqualTo(DomainReceiptVoucherStatus.Approved));

        await handler.Handle(
            new SetReceiptVoucherStatusCommand(
                voucher.Id,
                new SetReceiptVoucherStatusRequest(ReceiptVoucherStatus.Posted, rowVersion)),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(voucher.Status, Is.EqualTo(DomainReceiptVoucherStatus.Posted));
            Assert.That(voucher.JournalEntryId, Is.EqualTo(posting.JournalEntryId));
            Assert.That(voucher.PostedBy, Is.EqualTo(userId));
            Assert.That(voucher.PostedAtUtc, Is.Not.Null);
            Assert.That(posting.PostReceiptVoucherCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task PaymentVoucherStatusHandler_ApprovesThenPostsAndAssignsJournal()
    {
        var userId = Guid.NewGuid();
        var voucher = PaymentVoucher.Create(
            Guid.NewGuid(), "PV-2026-STATUS", new DateOnly(2026, 2, 1),
            OAS.Domain.Accounting.Enums.PaymentPartyType.Other, null, "طرف", DomainPaymentMethod.Other,
            null, null, 100m, DomainPaymentVoucherStatus.Draft, "اختبار دورة الحالة", null);
        var line = PaymentVoucherLine.Create(
            Guid.NewGuid(), voucher.Id, 1, _counterpartyAccountId, 100m, null, null, "سطر");

        await _paymentRepository.AddAsync(voucher);
        await _paymentLineRepository.AddAsync(line);

        var posting = new FakeAccountingDocumentPostingService();
        var handler = new SetPaymentVoucherStatusCommandHandler(
            _paymentRepository, _paymentLineRepository, posting, new FakePermissionChecker(),
            new FakeCurrentUser { UserId = userId.ToString() }, TimeProvider.System);

        var rowVersion = Convert.ToBase64String(voucher.RowVersion);
        await handler.Handle(
            new SetPaymentVoucherStatusCommand(
                voucher.Id,
                new SetPaymentVoucherStatusRequest(PaymentVoucherStatus.Approved, rowVersion)),
            CancellationToken.None);

        Assert.That(voucher.Status, Is.EqualTo(DomainPaymentVoucherStatus.Approved));

        await handler.Handle(
            new SetPaymentVoucherStatusCommand(
                voucher.Id,
                new SetPaymentVoucherStatusRequest(PaymentVoucherStatus.Posted, rowVersion)),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(voucher.Status, Is.EqualTo(DomainPaymentVoucherStatus.Posted));
            Assert.That(voucher.JournalEntryId, Is.EqualTo(posting.JournalEntryId));
            Assert.That(voucher.PostedBy, Is.EqualTo(userId));
            Assert.That(voucher.PostedAtUtc, Is.Not.Null);
            Assert.That(posting.PostPaymentVoucherCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReceiptVoucherStatusHandler_RejectsDraftToPostedTransition()
    {
        var voucher = ReceiptVoucher.Create(
            Guid.NewGuid(), "RV-2026-INVALID", new DateOnly(2026, 2, 1),
            OAS.Domain.Accounting.Enums.ReceiptPartyType.Other, null, "طرف", DomainPaymentMethod.Other,
            null, null, 100m, DomainReceiptVoucherStatus.Draft, null, null);

        _receiptRepository.AddAsync(voucher).GetAwaiter().GetResult();

        var handler = new SetReceiptVoucherStatusCommandHandler(
            _receiptRepository, _receiptLineRepository, new FakeAccountingDocumentPostingService(),
            new FakePermissionChecker(), new FakeCurrentUser { UserId = Guid.NewGuid().ToString() },
            TimeProvider.System);

        var request = new SetReceiptVoucherStatusCommand(
            voucher.Id,
            new SetReceiptVoucherStatusRequest(
                ReceiptVoucherStatus.Posted,
                Convert.ToBase64String(voucher.RowVersion)));

        Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(request, CancellationToken.None));
    }

    [Test]
    public void PaymentVoucherStatusHandler_RejectsDraftToPostedTransition()
    {
        var voucher = PaymentVoucher.Create(
            Guid.NewGuid(), "PV-2026-INVALID", new DateOnly(2026, 2, 1),
            OAS.Domain.Accounting.Enums.PaymentPartyType.Other, null, "طرف", DomainPaymentMethod.Other,
            null, null, 100m, DomainPaymentVoucherStatus.Draft, null, null);

        _paymentRepository.AddAsync(voucher).GetAwaiter().GetResult();

        var handler = new SetPaymentVoucherStatusCommandHandler(
            _paymentRepository, _paymentLineRepository, new FakeAccountingDocumentPostingService(),
            new FakePermissionChecker(), new FakeCurrentUser { UserId = Guid.NewGuid().ToString() },
            TimeProvider.System);

        var request = new SetPaymentVoucherStatusCommand(
            voucher.Id,
            new SetPaymentVoucherStatusRequest(
                PaymentVoucherStatus.Posted,
                Convert.ToBase64String(voucher.RowVersion)));

        Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(request, CancellationToken.None));
    }

    [Test]
    public async Task ReceiptVoucherStatusHandler_ApprovedVoucher_CanBeCancelled()
    {
        var voucher = ReceiptVoucher.Create(
            Guid.NewGuid(), "RV-2026-CANCEL", new DateOnly(2026, 2, 1),
            OAS.Domain.Accounting.Enums.ReceiptPartyType.Other, null, "طرف", DomainPaymentMethod.Other,
            null, null, 100m, DomainReceiptVoucherStatus.Draft, null, null);
        var line = ReceiptVoucherLine.Create(
            Guid.NewGuid(), voucher.Id, 1, _counterpartyAccountId, 100m, null, null, "سطر");

        await _receiptRepository.AddAsync(voucher);
        await _receiptLineRepository.AddAsync(line);

        var handler = new SetReceiptVoucherStatusCommandHandler(
            _receiptRepository, _receiptLineRepository, new FakeAccountingDocumentPostingService(),
            new FakePermissionChecker(), new FakeCurrentUser { UserId = Guid.NewGuid().ToString() },
            TimeProvider.System);
        var rowVersion = Convert.ToBase64String(voucher.RowVersion);

        await handler.Handle(
            new SetReceiptVoucherStatusCommand(
                voucher.Id,
                new SetReceiptVoucherStatusRequest(ReceiptVoucherStatus.Approved, rowVersion)),
            CancellationToken.None);
        await handler.Handle(
            new SetReceiptVoucherStatusCommand(
                voucher.Id,
                new SetReceiptVoucherStatusRequest(ReceiptVoucherStatus.Cancelled, rowVersion)),
            CancellationToken.None);

        Assert.That(voucher.Status, Is.EqualTo(DomainReceiptVoucherStatus.Cancelled));
    }

    [Test]
    public async Task PaymentVoucherStatusHandler_ApprovedVoucher_CanBeCancelled()
    {
        var voucher = PaymentVoucher.Create(
            Guid.NewGuid(), "PV-2026-CANCEL", new DateOnly(2026, 2, 1),
            OAS.Domain.Accounting.Enums.PaymentPartyType.Other, null, "طرف", DomainPaymentMethod.Other,
            null, null, 100m, DomainPaymentVoucherStatus.Draft, null, null);
        var line = PaymentVoucherLine.Create(
            Guid.NewGuid(), voucher.Id, 1, _counterpartyAccountId, 100m, null, null, "سطر");

        await _paymentRepository.AddAsync(voucher);
        await _paymentLineRepository.AddAsync(line);

        var handler = new SetPaymentVoucherStatusCommandHandler(
            _paymentRepository, _paymentLineRepository, new FakeAccountingDocumentPostingService(),
            new FakePermissionChecker(), new FakeCurrentUser { UserId = Guid.NewGuid().ToString() },
            TimeProvider.System);
        var rowVersion = Convert.ToBase64String(voucher.RowVersion);

        await handler.Handle(
            new SetPaymentVoucherStatusCommand(
                voucher.Id,
                new SetPaymentVoucherStatusRequest(PaymentVoucherStatus.Approved, rowVersion)),
            CancellationToken.None);
        await handler.Handle(
            new SetPaymentVoucherStatusCommand(
                voucher.Id,
                new SetPaymentVoucherStatusRequest(PaymentVoucherStatus.Cancelled, rowVersion)),
            CancellationToken.None);

        Assert.That(voucher.Status, Is.EqualTo(DomainPaymentVoucherStatus.Cancelled));
    }

    [Test]
    public async Task GetReceiptVoucherByIdQueryHandler_ReturnsSettlementSnapshot()
    {
        var voucher = ReceiptVoucher.CreateSettlementDocument(Guid.NewGuid(), "RV-2026-000001", new DateOnly(2026, 1, 15), _currencyId, "YER", 2, 1500m, "سند");
        await _receiptRepository.AddAsync(voucher);
        var line = ReceiptVoucherLine.CreateSettlement(Guid.NewGuid(), voucher.Id, 1, DomainPartyType.Other, null, null, null,
            "عميل نقدي", _counterpartyAccountId, DomainPaymentMethod.Cash, Guid.NewGuid(), null, _settlementAccountId,
            _currencyId, "YER", "ر.ي", 2, 1500m, 1m, voucher.VoucherDate, DomainExchangeRateType.Accounting,
            DomainExchangeRateSource.System, 1500m, null, null, null, null, "إيراد");
        await _receiptLineRepository.AddAsync(line);

        var dto = await new GetReceiptVoucherByIdQueryHandler(_receiptRepository, _receiptLineRepository, new ReceiptVoucherMapper())
            .Handle(new GetReceiptVoucherByIdQuery(voucher.Id), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(dto.BaseTotalAmount, Is.EqualTo(1500m));
            Assert.That(dto.Lines, Has.Count.EqualTo(1));
            Assert.That(dto.Lines[0].CurrencyCodeSnapshot, Is.EqualTo("YER"));
            Assert.That(dto.Lines[0].BaseAmount, Is.EqualTo(1500m));
        });
    }

    [Test]
    public async Task GetPaymentVoucherByIdQueryHandler_ReturnsSettlementSnapshot()
    {
        var voucher = PaymentVoucher.CreateSettlementDocument(Guid.NewGuid(), "PV-2026-000001", new DateOnly(2026, 1, 15), _currencyId, "YER", 2, 2200m, "سند صرف");
        await _paymentRepository.AddAsync(voucher);
        var line = PaymentVoucherLine.CreateSettlement(Guid.NewGuid(), voucher.Id, 1, DomainPartyType.Other, null, null, null,
            "مورد", _counterpartyAccountId, DomainPaymentMethod.BankTransfer, null, Guid.NewGuid(), _settlementAccountId,
            _currencyId, "YER", "ر.ي", 2, 2200m, 1m, voucher.VoucherDate, DomainExchangeRateType.Accounting,
            DomainExchangeRateSource.System, 2200m, null, null, null, null, "مصروف");
        await _paymentLineRepository.AddAsync(line);

        var dto = await new GetPaymentVoucherByIdQueryHandler(_paymentRepository, _paymentLineRepository, new PaymentVoucherMapper())
            .Handle(new GetPaymentVoucherByIdQuery(voucher.Id), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(dto.BaseTotalAmount, Is.EqualTo(2200m));
            Assert.That(dto.Lines, Has.Count.EqualTo(1));
            Assert.That(dto.Lines[0].CurrencyCodeSnapshot, Is.EqualTo("YER"));
        });
    }

    private sealed class FakeAccountingDocumentPostingService : IAccountingDocumentPostingService
    {
        public Guid JournalEntryId { get; } = Guid.NewGuid();
        public int PostReceiptVoucherCallCount { get; private set; }
        public int PostPaymentVoucherCallCount { get; private set; }

        public Task<Guid> PostReceiptVoucherAsync(
            ReceiptVoucher voucher, IReadOnlyCollection<ReceiptVoucherLine> lines, Guid postedBy,
            DateTime postedAtUtc, CancellationToken cancellationToken = default)
        {
            PostReceiptVoucherCallCount++;
            return Task.FromResult(JournalEntryId);
        }

        public Task<Guid> PostPaymentVoucherAsync(
            PaymentVoucher voucher, IReadOnlyCollection<PaymentVoucherLine> lines, Guid postedBy,
            DateTime postedAtUtc, CancellationToken cancellationToken = default)
        {
            PostPaymentVoucherCallCount++;
            return Task.FromResult(JournalEntryId);
        }

        public Task<Guid> PostExpenseAsync(
            Expense expense, Guid postedBy, DateTime postedAtUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(JournalEntryId);
    }

    private sealed class FakeSettlementResolver(Guid counterpartyAccountId, Guid settlementAccountId, Guid currencyId, decimal rate) : IVoucherSettlementResolver
    {
        public Task<VoucherSettlementResolution> ResolveAsync(
            DateOnly voucherDate,
            DomainPartyType partyType,
            Guid? customerId,
            Guid? supplierId,
            Guid? employeeId,
            string? partyName,
            Guid? otherCounterpartyAccountId,
            DomainPaymentMethod paymentMethod,
            Guid? cashAccountId,
            Guid? bankAccountId,
            Guid? otherSettlementAccountId,
            Guid requestedCurrencyId,
            decimal amount,
            decimal? manualExchangeRate,
            DomainExchangeRateType exchangeRateType,
            string? referenceNumber,
            CancellationToken cancellationToken = default)
        {
            var effectiveRate = manualExchangeRate is > 0 ? manualExchangeRate.Value : rate;
            return Task.FromResult(new VoucherSettlementResolution(
                partyType,
                customerId,
                supplierId,
                employeeId,
                string.IsNullOrWhiteSpace(partyName) ? "طرف" : partyName,
                otherCounterpartyAccountId ?? counterpartyAccountId,
                paymentMethod,
                cashAccountId,
                bankAccountId,
                otherSettlementAccountId ?? settlementAccountId,
                requestedCurrencyId == Guid.Empty ? currencyId : requestedCurrencyId,
                "YER",
                "ر.ي",
                2,
                amount,
                effectiveRate,
                voucherDate,
                exchangeRateType,
                manualExchangeRate is > 0 ? DomainExchangeRateSource.Manual : DomainExchangeRateSource.System,
                amount * effectiveRate));
        }
    }
}
