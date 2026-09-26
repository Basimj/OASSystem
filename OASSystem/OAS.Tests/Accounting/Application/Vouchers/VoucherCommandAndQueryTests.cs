using NUnit.Framework;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.PaymentVouchers.Commands.CreatePaymentVoucher;
using OAS.Application.Accounting.PaymentVouchers.Mapping;
using OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVoucherById;
using OAS.Application.Accounting.ReceiptVouchers.Commands.CreateReceiptVoucher;
using OAS.Application.Accounting.ReceiptVouchers.Mapping;
using OAS.Application.Accounting.ReceiptVouchers.Queries.GetReceiptVoucherById;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;
using DomainExchangeRateSource = OAS.Domain.Accounting.Enums.ExchangeRateSource;
using DomainExchangeRateType = OAS.Domain.Accounting.Enums.ExchangeRateType;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;
using DomainPartyType = OAS.Domain.Accounting.Enums.SettlementPartyType;

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
