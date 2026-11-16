using System.Text.Json;
using NUnit.Framework;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.Returns.Queries;
using OAS.Client.Sales.Services;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class CustomerCreditRetryTests
{
    [Test]
    public void ApplySnapshot_RoundTripsWithIdenticalKeyAndPayload()
    {
        var customer = Guid.NewGuid();
        var pending = CustomerCreditPendingOperation.ForApply(customer, Guid.NewGuid(), Guid.NewGuid(), 200m);
        var restored = JsonSerializer.Deserialize<CustomerCreditPendingOperation>(
            JsonSerializer.Serialize(pending));

        Assert.That(restored, Is.EqualTo(pending));
        Assert.That(restored!.IsValidFor(customer), Is.True);
        Assert.That(restored.IdempotencyKey, Is.EqualTo(pending.Apply!.IdempotencyKey));
        Assert.That(restored.IsValidFor(Guid.NewGuid()), Is.False);
    }

    [Test]
    public void RefundSnapshot_RoundTripsWithIdenticalKeyAndPaymentDetails()
    {
        var customer = Guid.NewGuid();
        var pending = CustomerCreditPendingOperation.ForRefund(customer, Guid.NewGuid(),
            Guid.NewGuid(), 200m, 1, Guid.NewGuid(), null);
        var restored = JsonSerializer.Deserialize<CustomerCreditPendingOperation>(
            JsonSerializer.Serialize(pending));

        Assert.That(restored, Is.EqualTo(pending));
        Assert.That(restored!.IsValidFor(customer), Is.True);
        Assert.That(restored.IdempotencyKey, Is.EqualTo(pending.Refund!.IdempotencyKey));
        Assert.That(restored.Refund!.Amount, Is.EqualTo(200m));
        Assert.That(restored.Refund.CashAccountId, Is.EqualTo(pending.Refund.CashAccountId));
    }

    [Test]
    public void PendingStorageKey_SharesCustomerAndProfileAcrossSalesScreens()
    {
        var customer = Guid.NewGuid();
        var shared = CustomerCreditPendingOperation.StorageKeyFor(customer, "mydb");
        Assert.That(shared, Does.Contain(customer.ToString("N")));
        Assert.That(shared, Is.EqualTo(CustomerCreditPendingOperation.StorageKeyFor(customer, "mydb")));
        Assert.That(shared, Is.Not.EqualTo(CustomerCreditPendingOperation.StorageKeyFor(customer, "anotherdb")));
        Assert.That(shared, Is.Not.EqualTo(CustomerCreditPendingOperation.StorageKeyFor(Guid.NewGuid(), "mydb")));
    }

    [Test]
    public void DamagedOrMismatchedSnapshotsCannotBeRetried()
    {
        var customer = Guid.NewGuid();
        var pending = CustomerCreditPendingOperation.ForApply(customer, Guid.NewGuid(), Guid.NewGuid(), 10m);
        Assert.That((pending with { Kind = "refund" }).IsValidFor(customer), Is.False);
        Assert.That((pending with { Apply = pending.Apply! with { IdempotencyKey = null } })
            .IsValidFor(customer), Is.False);
        Assert.That((pending with { Refund = CustomerCreditPendingOperation.ForRefund(
            customer, Guid.NewGuid(), Guid.NewGuid(), 10m, 1, Guid.NewGuid(), null).Refund })
            .IsValidFor(customer), Is.False);
    }

    [Test]
    public void OperationLookup_RequiresCreditUsePermission()
    {
        var query = new GetCustomerCreditOperationQuery(Guid.NewGuid(), Guid.NewGuid().ToString("N"));
        Assert.That(query.RequiredPermissions, Does.Contain(SalesPermissions.Credit.Use));
    }

    [Test]
    public void OperationLookup_RejectsInvalidKeyBeforeRepositoryAccess()
    {
        var handler = new GetCustomerCreditOperationQueryHandler(null!, null!, null!);
        Assert.ThrowsAsync<OAS.Application.Common.Exceptions.ConflictException>(async () =>
            await handler.Handle(new GetCustomerCreditOperationQuery(Guid.NewGuid(), ""), CancellationToken.None));
    }
}
