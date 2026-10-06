using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public sealed class CustomerAdvanceFoundationDomainTests
{
    [Test]
    public void PartialThenFullApplication_UpdatesBalanceAndStatus()
    {
        var advance = CreateAdvance(100m, 25000m);

        advance.Apply(40m, 10000m);
        Assert.Multiple(() =>
        {
            Assert.That(advance.Status, Is.EqualTo(CustomerAdvanceStatus.PartiallyApplied));
            Assert.That(advance.AvailableAmount, Is.EqualTo(60m));
            Assert.That(advance.BaseAvailableAmount, Is.EqualTo(15000m));
        });

        advance.Apply(60m, 15000m);
        Assert.Multiple(() =>
        {
            Assert.That(advance.Status, Is.EqualTo(CustomerAdvanceStatus.Applied));
            Assert.That(advance.AvailableAmount, Is.Zero);
            Assert.That(advance.BaseAvailableAmount, Is.Zero);
        });
    }

    [Test]
    public void ApplyingMoreThanAvailable_IsRejected()
    {
        var advance = CreateAdvance(100m, 25000m);
        Assert.Throws<InvalidOperationException>(() => advance.Apply(101m, 25250m));
    }

    [Test]
    public void ApplyingMoreThanAvailableBaseAmount_IsRejected()
    {
        var advance = CreateAdvance(100m, 25000m);
        advance.Apply(50m, 20000m);

        Assert.Throws<InvalidOperationException>(() => advance.Apply(10m, 6000m));
    }

    [Test]
    public void ReverseApplication_RestoresAvailableBalance()
    {
        var advance = CreateAdvance(100m, 25000m);
        advance.Apply(50m, 12500m);
        advance.ReverseApplication(50m, 12500m);

        Assert.Multiple(() =>
        {
            Assert.That(advance.Status, Is.EqualTo(CustomerAdvanceStatus.Available));
            Assert.That(advance.AvailableAmount, Is.EqualTo(100m));
        });
    }

    [Test]
    public void AppliedAdvance_CannotBeCancelledDirectly()
    {
        var advance = CreateAdvance(100m, 25000m);
        advance.Apply(10m, 2500m);
        Assert.Throws<InvalidOperationException>(advance.Cancel);
    }

    private static CustomerAdvance CreateAdvance(decimal amount, decimal baseAmount) => CustomerAdvance.Create(
        Guid.NewGuid(), "ADV-TEST-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        Guid.NewGuid(), "USD", "$", 2, Guid.NewGuid(), "YER", 2,
        amount, 250m, new DateOnly(2026, 10, 4), ExchangeRateType.Accounting,
        ExchangeRateSource.System, baseAmount, new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));
}
