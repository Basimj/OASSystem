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
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;
using DomainPaymentSourceType = OAS.Domain.Accounting.Enums.PaymentSourceType;
using DomainReceiptPartyType = OAS.Domain.Accounting.Enums.ReceiptPartyType;
using DomainReceiptVoucherStatus = OAS.Domain.Accounting.Enums.ReceiptVoucherStatus;

namespace OAS.Tests.Accounting.Application.PaymentAllocations;

[TestFixture]
public class PaymentAllocationCommandAndQueryTests
{
    private FakeRepository<PaymentAllocation, Guid> _allocationRepository = null!;
    private FakeRepository<ReceiptVoucher, Guid> _receiptRepository = null!;
    private FakeRepository<PaymentVoucher, Guid> _paymentRepository = null!;
    private FakeCurrentUser _currentUser = null!;
    private PaymentAllocationMapper _mapper = null!;

    [SetUp]
    public void Setup()
    {
        _allocationRepository = new FakeRepository<PaymentAllocation, Guid>();
        _receiptRepository = new FakeRepository<ReceiptVoucher, Guid>();
        _paymentRepository = new FakeRepository<PaymentVoucher, Guid>();
        _currentUser = new FakeCurrentUser();
        _mapper = new PaymentAllocationMapper();
    }

    [Test]
    public async Task CreatePaymentAllocationCommandHandler_CreatesAllocationWithinAvailableAmount()
    {
        var receipt = CreateReceiptVoucher(1000m);
        await _receiptRepository.AddAsync(receipt);

        await _allocationRepository.AddAsync(CreateAllocation(receipt.Id, 600m));

        var handler = new CreatePaymentAllocationCommandHandler(
            _allocationRepository,
            _receiptRepository,
            _paymentRepository,
            _currentUser,
            TimeProvider.System);

        var request = new CreatePaymentAllocationRequest(
            PaymentSourceType.ReceiptVoucher,
            receipt.Id,
            AllocationTargetDocumentType.SalesInvoice,
            Guid.NewGuid(),
            300m);

        var id = await handler.Handle(
            new CreatePaymentAllocationCommand(request),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(_allocationRepository.Items, Has.Count.EqualTo(2));
            Assert.That(_allocationRepository.Items.Sum(x => x.AllocatedAmount), Is.EqualTo(900m));
        });
    }

    [Test]
    public void CreatePaymentAllocationCommandHandler_WhenAmountExceedsAvailable_ThrowsConflictException()
    {
        var receipt = CreateReceiptVoucher(1000m);
        _receiptRepository.AddAsync(receipt).GetAwaiter().GetResult();
        _allocationRepository.AddAsync(CreateAllocation(receipt.Id, 600m)).GetAwaiter().GetResult();
        _allocationRepository.AddAsync(CreateAllocation(receipt.Id, 300m)).GetAwaiter().GetResult();

        var handler = new CreatePaymentAllocationCommandHandler(
            _allocationRepository,
            _receiptRepository,
            _paymentRepository,
            _currentUser,
            TimeProvider.System);

        var request = new CreatePaymentAllocationRequest(
            PaymentSourceType.ReceiptVoucher,
            receipt.Id,
            AllocationTargetDocumentType.SalesInvoice,
            Guid.NewGuid(),
            200m);

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(
                new CreatePaymentAllocationCommand(request),
                CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo("payment_allocation_exceeds_available_amount"));
        Assert.That(_allocationRepository.Items, Has.Count.EqualTo(2));
    }

    [Test]
    public void CreatePaymentAllocationCommandHandler_CustomerAdvanceWithoutSourcePort_ThrowsConflictException()
    {
        var handler = new CreatePaymentAllocationCommandHandler(
            _allocationRepository,
            _receiptRepository,
            _paymentRepository,
            _currentUser,
            TimeProvider.System);

        var request = new CreatePaymentAllocationRequest(
            PaymentSourceType.CustomerAdvance,
            Guid.NewGuid(),
            AllocationTargetDocumentType.SalesInvoice,
            Guid.NewGuid(),
            100m);

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(
                new CreatePaymentAllocationCommand(request),
                CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo("customer_advance_source_unavailable"));
    }

    [Test]
    public async Task UpdatePaymentAllocationCommandHandler_ExcludesCurrentAllocationFromAvailableCalculation()
    {
        var receipt = CreateReceiptVoucher(1000m);
        await _receiptRepository.AddAsync(receipt);

        var allocationToUpdate = CreateAllocation(receipt.Id, 400m);
        var otherAllocation = CreateAllocation(receipt.Id, 300m);
        await _allocationRepository.AddRangeAsync([allocationToUpdate, otherAllocation]);

        var handler = new UpdatePaymentAllocationCommandHandler(
            _allocationRepository,
            _receiptRepository,
            _paymentRepository);

        await handler.Handle(
            new UpdatePaymentAllocationCommand(
                allocationToUpdate.Id,
                new UpdatePaymentAllocationRequest(650m)),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(allocationToUpdate.AllocatedAmount, Is.EqualTo(650m));
            Assert.That(_allocationRepository.Items.Sum(x => x.AllocatedAmount), Is.EqualTo(950m));
        });
    }

    [Test]
    public void UpdatePaymentAllocationCommandHandler_WhenAmountExceedsAvailable_ThrowsConflictException()
    {
        var receipt = CreateReceiptVoucher(1000m);
        _receiptRepository.AddAsync(receipt).GetAwaiter().GetResult();

        var allocationToUpdate = CreateAllocation(receipt.Id, 400m);
        var otherAllocation = CreateAllocation(receipt.Id, 700m);
        _allocationRepository.AddRangeAsync([allocationToUpdate, otherAllocation]).GetAwaiter().GetResult();

        var handler = new UpdatePaymentAllocationCommandHandler(
            _allocationRepository,
            _receiptRepository,
            _paymentRepository);

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(
                new UpdatePaymentAllocationCommand(
                    allocationToUpdate.Id,
                    new UpdatePaymentAllocationRequest(400m)),
                CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo("payment_allocation_exceeds_available_amount"));
        Assert.That(allocationToUpdate.AllocatedAmount, Is.EqualTo(400m));
    }

    [Test]
    public async Task GetPaymentAllocationByIdQueryHandler_ReturnsMappedDto()
    {
        var allocation = PaymentAllocation.Create(
            Guid.NewGuid(),
            DomainPaymentSourceType.ReceiptVoucher,
            Guid.NewGuid(),
            DomainAllocationTargetDocumentType.SalesInvoice,
            Guid.NewGuid(),
            250m,
            DateTime.UtcNow,
            Guid.NewGuid());

        await _allocationRepository.AddAsync(allocation);

        var handler = new GetPaymentAllocationByIdQueryHandler(
            _allocationRepository,
            _mapper);

        var dto = await handler.Handle(
            new GetPaymentAllocationByIdQuery(allocation.Id),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(allocation.Id));
            Assert.That(dto.AllocatedAmount, Is.EqualTo(250m));
            Assert.That(dto.PaymentSourceId, Is.EqualTo(allocation.PaymentSourceId));
        });
    }

    private ReceiptVoucher CreateReceiptVoucher(decimal totalAmount) =>
        ReceiptVoucher.Create(
            Guid.NewGuid(),
            ($"RV-{Guid.NewGuid():N}")[..12],
            new DateOnly(2026, 1, 15),
            DomainReceiptPartyType.Other,
            null,
            "اختبار",
            DomainPaymentMethod.Cash,
            Guid.NewGuid(),
            null,
            totalAmount,
            DomainReceiptVoucherStatus.Posted,
            "سند اختبار",
            Guid.NewGuid(),
            Guid.Parse(_currentUser.UserId!),
            DateTime.UtcNow);

    private static PaymentAllocation CreateAllocation(Guid sourceId, decimal amount) =>
        PaymentAllocation.Create(
            Guid.NewGuid(),
            DomainPaymentSourceType.ReceiptVoucher,
            sourceId,
            DomainAllocationTargetDocumentType.SalesInvoice,
            Guid.NewGuid(),
            amount,
            DateTime.UtcNow,
            Guid.NewGuid());
}
