using NUnit.Framework;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Tests.Purchasing;

[TestFixture]
public sealed class SupplierCreditApplicationDomainTests
{
    [Test]
    public void Create_ThenReverse_PreservesAmountsAndAuditState()
    {
        var entity = SupplierCreditApplication.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            23m,
            23m,
            23m,
            DateTime.UtcNow,
            "tester",
            "PRC:test");

        entity.Reverse(DateTime.UtcNow, "reviewer", "wrong target invoice");

        Assert.Multiple(() =>
        {
            Assert.That(entity.Status, Is.EqualTo(SupplierCreditApplicationStatus.Reversed));
            Assert.That(entity.AppliedSourceBaseAmount, Is.EqualTo(23m));
            Assert.That(entity.AppliedTargetBaseAmount, Is.EqualTo(23m));
            Assert.That(entity.ReversalReason, Is.EqualTo("wrong target invoice"));
        });
    }

    [Test]
    public void Reverse_Twice_IsRejected()
    {
        var entity = SupplierCreditApplication.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            10m, 10m, 10m, DateTime.UtcNow, "tester", null);
        entity.Reverse(DateTime.UtcNow, "reviewer", "correction");

        Assert.Throws<DomainException>(() =>
            entity.Reverse(DateTime.UtcNow, "reviewer", "again"));
    }
}
