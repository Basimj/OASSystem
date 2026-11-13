using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.PurchaseReceipts.Commands;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Tests.Purchasing.Application;

[TestFixture]
public sealed class PurchaseReceiptPostingValueGuardTests
{
    [Test]
    public void ZeroCostAcceptedReceipt_StopsBeforeConfirmationOrPosting()
    {
        var receipt = Receipt(0m, 5m);
        var error = Assert.Throws<ConflictException>(() => PurchaseReceiptPostingValueGuard.EnsureValid(receipt));
        Assert.That(error!.Code, Is.EqualTo("purchasing_receipt_value_invalid"));
        Assert.That(error.Message, Does.Contain("التكلفة"));
    }

    [Test]
    public void ReceiptWithNoAcceptedUnits_CannotBeConfirmed()
    {
        var receipt = Receipt(10m, 0m);
        var error = Assert.Throws<ConflictException>(() => PurchaseReceiptPostingValueGuard.EnsureValid(receipt));
        Assert.That(error!.Code, Is.EqualTo("purchasing_receipt_no_accepted_quantity"));
    }

    [Test]
    public void ReceiptWithPositiveValue_Passes()
        => Assert.DoesNotThrow(() => PurchaseReceiptPostingValueGuard.EnsureValid(Receipt(20m, 5m)));

    private static PurchaseReceipt Receipt(decimal cost, decimal accepted)
    {
        var receipt = PurchaseReceipt.Create(Guid.NewGuid(), "GRN-GUARD", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 11, 11), new DateOnly(2026, 11, 11), null, null);
        receipt.AddLine(PurchaseReceiptLine.Create(Guid.NewGuid(), receipt.Id, Guid.NewGuid(), 1, Guid.NewGuid(),
            5m, 0m, 5m, accepted, 5m - accepted, 1m, cost, null, null, null));
        return receipt;
    }
}
