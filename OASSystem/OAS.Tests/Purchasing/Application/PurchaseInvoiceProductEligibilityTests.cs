using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.PurchaseInvoices.Services;

namespace OAS.Tests.Purchasing.Application;

[TestFixture]
public sealed class PurchaseInvoiceProductEligibilityTests
{
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(false, false)]
    public void InactiveProduct_WithLinkedPreviouslyPostedReceipt_IsAllowed(
        bool productIsActive, bool variantIsActive)
    {
        Assert.DoesNotThrow(() => PurchaseInvoiceProductEligibility.Validate(
            Product(productIsActive, variantIsActive), true, 2m));
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public void InactiveProduct_WithoutPostedReceipt_IsRejected(
        bool productIsActive, bool variantIsActive)
    {
        var exception = Assert.Throws<ConflictException>(() => PurchaseInvoiceProductEligibility.Validate(
            Product(productIsActive, variantIsActive), true, 0m));
        Assert.That(exception!.Code, Is.EqualTo("purchasing_invoice_inactive_product_without_posted_receipt"));
    }

    [Test]
    public void InactiveProduct_ManualInvoiceIsRejected_EvenIfOtherReceiptsExist()
    {
        var exception = Assert.Throws<ConflictException>(() => PurchaseInvoiceProductEligibility.Validate(
            Product(false, false), false, 7m));
        Assert.That(exception!.Code, Is.EqualTo("purchasing_invoice_inactive_product_without_posted_receipt"));
    }

    [Test]
    public void ActiveProduct_ManualInvoiceCanBePrepared()
    {
        Assert.DoesNotThrow(() => PurchaseInvoiceProductEligibility.Validate(Product(true, true), false, 0m));
    }

    [Test]
    public void MissingProduct_RemainsInvalid()
    {
        Assert.Throws<RequestValidationException>(() =>
            PurchaseInvoiceProductEligibility.Validate(null, true, 4m));
    }

    [Test]
    public void NonStockProduct_RemainsInvalid()
    {
        var snapshot = Product(true, true) with { IsStockItem = false };
        var exception = Assert.Throws<ConflictException>(() =>
            PurchaseInvoiceProductEligibility.Validate(snapshot, true, 2m));
        Assert.That(exception!.Code, Is.EqualTo("purchasing_product_not_stock_item"));
    }

    private static PurchasingProductSnapshot Product(bool productIsActive, bool variantIsActive) =>
        new(Guid.NewGuid(), "P-001", "صنف مخزني", variantIsActive, productIsActive, true, Guid.NewGuid());
}
