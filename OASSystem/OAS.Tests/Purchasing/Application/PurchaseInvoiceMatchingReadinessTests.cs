using NUnit.Framework;
using OAS.Application.Purchasing.PurchaseInvoices.Services;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using ContractMatchStatus = OAS.Contracts.Purchasing.Enums.PurchaseMatchStatus;
using InvoiceStatus = OAS.Domain.Purchasing.Enums.PurchaseInvoiceStatus;

namespace OAS.Tests.Purchasing.Application;

[TestFixture]
public sealed class PurchaseInvoiceMatchingReadinessTests
{
    [Test]
    public void NoSavedAllocations_ExplainsMatchingIsRequired()
    {
        var result = PurchaseInvoiceMatchingReadiness.Evaluate(InvoiceStatus.Confirmed, []);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.Code, Is.EqualTo("purchasing_invoice_matching_required"));
            Assert.That(result.Message, Does.Contain("تشغيل المطابقة"));
            Assert.That(result.AllocationCount, Is.Zero);
        });
    }

    [TestCase(ContractMatchStatus.Matched)]
    [TestCase(ContractMatchStatus.WithinTolerance)]
    [TestCase(ContractMatchStatus.ApprovedVariance)]
    public void SavedApprovedMatching_IsComplete(ContractMatchStatus status)
    {
        var result = PurchaseInvoiceMatchingReadiness.Evaluate(InvoiceStatus.Confirmed, [Allocation(status)]);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.UnresolvedCount, Is.Zero);
            Assert.That(result.Allocations, Has.Count.EqualTo(1));
        });
    }

    [TestCase(ContractMatchStatus.Pending)]
    [TestCase(ContractMatchStatus.RequiresApproval)]
    [TestCase(ContractMatchStatus.Rejected)]
    public void UnresolvedMatching_IsNotReady(ContractMatchStatus status)
    {
        var result = PurchaseInvoiceMatchingReadiness.Evaluate(InvoiceStatus.Confirmed, [Allocation(status)]);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.Code, Is.EqualTo("purchasing_invoice_matching_incomplete"));
            Assert.That(result.UnresolvedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void PendingApprovalStatus_BlocksEvenIfSavedAllocationsWereApproved()
    {
        var result = PurchaseInvoiceMatchingReadiness.Evaluate(InvoiceStatus.PendingMatchApproval,
            [Allocation(ContractMatchStatus.ApprovedVariance)]);
        Assert.That(result.Code, Is.EqualTo("purchasing_invoice_variance_approval_required"));
        Assert.That(result.IsComplete, Is.False);
    }

    private static PurchaseInvoiceReceiptAllocationDto Allocation(ContractMatchStatus status) => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
        2m, 400m, 0m, 0m, 0m, status, null, null, null, "dGVzdA==");
}
