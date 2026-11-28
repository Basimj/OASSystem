using NUnit.Framework;
using OAS.Application.Accounting.CashAccounts;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public sealed class CashAccountMovementTypeTests
{
    private static CashAccount Box() => CashAccount.Create(Guid.NewGuid(), "BOX", "Test", Guid.NewGuid(), false, true);

    [TestCase(CashAccountMovementType.ReceiptOnly, true, false)]
    [TestCase(CashAccountMovementType.PaymentOnly, false, true)]
    [TestCase(CashAccountMovementType.ReceiptAndPayment, true, true)]
    public void DirectionPolicy(CashAccountMovementType movement, bool receipts, bool payments)
    {
        var box = Box();
        box.SetMovementType(movement);
        Assert.That(box.AllowsReceipt, Is.EqualTo(receipts));
        Assert.That(box.AllowsPayment, Is.EqualTo(payments));
        if (receipts) Assert.DoesNotThrow(() => CashAccountMovementPolicy.EnsureAllowed(box, true));
        else Assert.Throws<ConflictException>(() => CashAccountMovementPolicy.EnsureAllowed(box, true));
        if (payments) Assert.DoesNotThrow(() => CashAccountMovementPolicy.EnsureAllowed(box, false));
        else Assert.Throws<ConflictException>(() => CashAccountMovementPolicy.EnsureAllowed(box, false));
    }

    [Test]
    public void DefaultIsBothAndInvalidValuesAreRejected()
    {
        var box = Box();
        Assert.That(box.MovementType, Is.EqualTo(CashAccountMovementType.ReceiptAndPayment));
        Assert.Throws<ArgumentOutOfRangeException>(() => box.SetMovementType((CashAccountMovementType)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => box.SetMovementType((CashAccountMovementType)4));
    }
}
