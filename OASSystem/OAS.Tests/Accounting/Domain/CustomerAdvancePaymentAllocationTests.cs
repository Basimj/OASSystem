using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public sealed class CustomerAdvancePaymentAllocationTests
{
    [Test]
    public void CustomerAdvanceAllocation_UsesTypedApplicationSource()
    {
        var applicationId = Guid.NewGuid();
        var allocation = PaymentAllocation.CreateCustomerAdvanceAllocation(
            Guid.NewGuid(), applicationId, AllocationTargetDocumentType.SalesInvoice,
            Guid.NewGuid(), Guid.NewGuid(), "USD", 25m, 250m, 6250m, 6250m,
            new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));

        Assert.Multiple(() =>
        {
            Assert.That(allocation.PaymentSourceType, Is.EqualTo(PaymentSourceType.CustomerAdvance));
            Assert.That(allocation.PaymentSourceId, Is.EqualTo(applicationId));
            Assert.That(allocation.CustomerAdvanceApplicationId, Is.EqualTo(applicationId));
            Assert.That(allocation.ReceiptVoucherLineId, Is.Null);
            Assert.That(allocation.PaymentVoucherLineId, Is.Null);
        });
    }

    [Test]
    public void TypedAllocation_RejectsMoreThanOneSource()
    {
        Assert.Throws<InvalidOperationException>(() => PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), AllocationTargetDocumentType.SalesInvoice,
            Guid.NewGuid(), Guid.NewGuid(), "USD", 10m, 250m, 2500m,
            new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc)));
    }
}
