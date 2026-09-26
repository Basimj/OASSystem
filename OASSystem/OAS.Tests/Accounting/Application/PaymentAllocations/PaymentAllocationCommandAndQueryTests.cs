using NUnit.Framework;
using OAS.Application.Accounting.PaymentAllocations.Commands.CreatePaymentAllocation;
using OAS.Application.Accounting.PaymentAllocations.Commands.UpdatePaymentAllocation;
using OAS.Application.Accounting.PaymentAllocations.Mapping;
using OAS.Application.Accounting.PaymentAllocations.Queries.GetPaymentAllocationById;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.PaymentAllocations;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;
using DomainAllocationTargetDocumentType = OAS.Domain.Accounting.Enums.AllocationTargetDocumentType;
using DomainExchangeRateSource = OAS.Domain.Accounting.Enums.ExchangeRateSource;
using DomainExchangeRateType = OAS.Domain.Accounting.Enums.ExchangeRateType;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;
using DomainSettlementPartyType = OAS.Domain.Accounting.Enums.SettlementPartyType;

namespace OAS.Tests.Accounting.Application.PaymentAllocations;

[TestFixture]
public sealed class PaymentAllocationCommandAndQueryTests
{
    private FakeRepository<PaymentAllocation, Guid> _allocationRepository = null!;
    private FakeRepository<ReceiptVoucherLine, Guid> _receiptLineRepository = null!;
    private FakeRepository<PaymentVoucherLine, Guid> _paymentLineRepository = null!;
    private Guid _currencyId;
    private Guid _accountId;
    private Guid _settlementAccountId;

    [SetUp]
    public void Setup()
    {
        _allocationRepository = new();
        _receiptLineRepository = new();
        _paymentLineRepository = new();
        _currencyId = Guid.NewGuid();
        _accountId = Guid.NewGuid();
        _settlementAccountId = Guid.NewGuid();
    }

    [Test]
    public async Task CreatePaymentAllocationCommandHandler_CreatesLineLevelAllocation()
    {
        var source = CreateReceiptLine(1000m);
        await _receiptLineRepository.AddAsync(source);
        await _allocationRepository.AddAsync(PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(), source.Id, null, DomainAllocationTargetDocumentType.SalesInvoice, Guid.NewGuid(), _currencyId, "USD", 600m, 550m, 330000m, DateTime.UtcNow));

        var handler = new CreatePaymentAllocationCommandHandler(_allocationRepository, _receiptLineRepository, _paymentLineRepository, TimeProvider.System);
        var request = new CreatePaymentAllocationRequest(source.Id, null, AllocationTargetDocumentType.SalesInvoice, Guid.NewGuid(), 300m);

        var id = await handler.Handle(new CreatePaymentAllocationCommand(request), CancellationToken.None);
        var created = _allocationRepository.Items.Single(x => x.Id == id);

        Assert.Multiple(() =>
        {
            Assert.That(created.ReceiptVoucherLineId, Is.EqualTo(source.Id));
            Assert.That(created.PaymentVoucherLineId, Is.Null);
            Assert.That(created.CurrencyId, Is.EqualTo(_currencyId));
            Assert.That(created.AllocatedAmount, Is.EqualTo(300m));
            Assert.That(created.BaseAllocatedAmount, Is.EqualTo(165000m));
        });
    }

    [Test]
    public void CreatePaymentAllocationCommandHandler_WhenAmountExceedsAvailable_ThrowsConflictException()
    {
        var source = CreateReceiptLine(1000m);
        _receiptLineRepository.AddAsync(source).GetAwaiter().GetResult();
        _allocationRepository.AddAsync(PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(), source.Id, null, DomainAllocationTargetDocumentType.SalesInvoice, Guid.NewGuid(), _currencyId, "USD", 900m, 550m, 495000m, DateTime.UtcNow)).GetAwaiter().GetResult();

        var handler = new CreatePaymentAllocationCommandHandler(_allocationRepository, _receiptLineRepository, _paymentLineRepository, TimeProvider.System);
        var request = new CreatePaymentAllocationRequest(source.Id, null, AllocationTargetDocumentType.SalesInvoice, Guid.NewGuid(), 200m);

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(new CreatePaymentAllocationCommand(request), CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo("payment_allocation_exceeds_available_amount"));
    }

    [Test]
    public async Task UpdatePaymentAllocationCommandHandler_ExcludesCurrentAllocationFromAvailableCalculation()
    {
        var source = CreateReceiptLine(1000m);
        await _receiptLineRepository.AddAsync(source);
        var current = PaymentAllocation.CreateLineAllocation(Guid.NewGuid(), source.Id, null, DomainAllocationTargetDocumentType.SalesInvoice, Guid.NewGuid(), _currencyId, "USD", 400m, 550m, 220000m, DateTime.UtcNow);
        var other = PaymentAllocation.CreateLineAllocation(Guid.NewGuid(), source.Id, null, DomainAllocationTargetDocumentType.SalesInvoice, Guid.NewGuid(), _currencyId, "USD", 300m, 550m, 165000m, DateTime.UtcNow);
        await _allocationRepository.AddRangeAsync([current, other]);

        var handler = new UpdatePaymentAllocationCommandHandler(_allocationRepository, _receiptLineRepository, _paymentLineRepository);
        await handler.Handle(new UpdatePaymentAllocationCommand(current.Id, new UpdatePaymentAllocationRequest(650m)), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(current.AllocatedAmount, Is.EqualTo(650m));
            Assert.That(current.BaseAllocatedAmount, Is.EqualTo(357500m));
        });
    }

    [Test]
    public async Task GetPaymentAllocationByIdQueryHandler_ReturnsMappedDto()
    {
        var source = CreateReceiptLine(1000m);
        var allocation = PaymentAllocation.CreateLineAllocation(Guid.NewGuid(), source.Id, null, DomainAllocationTargetDocumentType.SalesInvoice, Guid.NewGuid(), _currencyId, "USD", 250m, 550m, 137500m, DateTime.UtcNow);
        await _allocationRepository.AddAsync(allocation);

        var dto = await new GetPaymentAllocationByIdQueryHandler(_allocationRepository, new PaymentAllocationMapper())
            .Handle(new GetPaymentAllocationByIdQuery(allocation.Id), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(allocation.Id));
            Assert.That(dto.ReceiptVoucherLineId, Is.EqualTo(source.Id));
            Assert.That(dto.CurrencyCodeSnapshot, Is.EqualTo("USD"));
            Assert.That(dto.BaseAllocatedAmount, Is.EqualTo(137500m));
        });
    }

    private ReceiptVoucherLine CreateReceiptLine(decimal amount)
    {
        var voucherId = Guid.NewGuid();
        return ReceiptVoucherLine.CreateSettlement(
            Guid.NewGuid(), voucherId, 1, DomainSettlementPartyType.Other, null, null, null,
            "طرف", _accountId, DomainPaymentMethod.Cash, Guid.NewGuid(), null, _settlementAccountId,
            _currencyId, "USD", "$", 2, amount, 550m, new DateOnly(2026, 1, 15), DomainExchangeRateType.Accounting,
            DomainExchangeRateSource.System, amount * 550m, null, null, null, null, null);
    }
}
