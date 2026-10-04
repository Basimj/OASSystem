using NUnit.Framework;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Sales.Commissions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Production;

namespace OAS.Tests.Integration;

[TestFixture]
public sealed class CommercialCycleEndToEndWorkflowTests
{
    [Test]
    public void PostedSale_Return_CommissionAndProductionRemake_RemainConsistent()
    {
        var now = new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
        var date = new DateOnly(2026, 10, 3);
        var currencyId = Guid.NewGuid();
        var invoice = SalesInvoice.Create(
            Guid.NewGuid(), "SI-E2E-001", Guid.NewGuid(), null, null,
            date, date, currencyId, "YER", "ر.ي", 2, 1m, date,
            ExchangeRateType.Accounting, ExchangeRateSource.System,
            TaxCalculationMode.Exclusive, SalesPaymentTermType.Immediate, 0,
            currencyId, "YER", 2, "commercial cycle test");

        var invoiceLine = SalesInvoiceLine.Create(
            Guid.NewGuid(), invoice.Id, 1, null, null,
            SalesLineType.Frame, Guid.NewGuid(), Guid.NewGuid(), "FR-E2E",
            "إطار اختباري", "إطار اختباري", "قطعة",
            2m, 100m, 100m, SalesDiscountType.None, null, 10m,
            null, null, true, null,
            TaxCalculationMode.Exclusive, 2, 1m, 2);
        invoice.AddLine(invoiceLine);
        invoice.Confirm(now, "tester");
        invoice.SetLineCostSnapshot(invoiceLine.Id, 45m, 90m);
        invoice.SetJournalEntry(Guid.NewGuid());
        invoice.Post(now, "tester");

        Assert.That(invoice.Status, Is.EqualTo(SalesInvoiceStatus.Posted));
        Assert.That(invoiceLine.TotalCostSnapshot, Is.EqualTo(90m));

        invoiceLine.ReserveReturnQuantity(1m);
        var salesReturn = SalesReturn.Create(
            Guid.NewGuid(), "SR-E2E-001", invoice.Id, invoice.CustomerId,
            date, date, currencyId, "YER", 2, 1m, date,
            ExchangeRateType.Accounting, ExchangeRateSource.System,
            currencyId, "YER", 2, "partial return");
        salesReturn.AddLine(SalesReturnLine.Create(
            Guid.NewGuid(), salesReturn.Id, 1, invoiceLine.Id,
            invoiceLine.LineType, invoiceLine.ProductVariantId, invoiceLine.WarehouseId,
            invoiceLine.ProductCodeSnapshot, invoiceLine.ProductNameSnapshot,
            1m, 100m, 10m, 110m, 100m, 10m, 110m, 45m, 45m));
        salesReturn.Confirm(now, "tester");
        salesReturn.MarkPosted(Guid.NewGuid(), now, "tester");

        Assert.That(salesReturn.Status, Is.EqualTo(SalesReturnStatus.Posted));
        Assert.That(invoiceLine.RemainingReturnableQuantity, Is.EqualTo(1m));
        Assert.That(salesReturn.BaseTotalAmount, Is.EqualTo(110m));

        var employeeId = Guid.NewGuid();
        var rule = CommissionRule.Create(Guid.NewGuid(), "COM-E2E", "عمولة اختبار", employeeId, 5m, date);
        var statement = CommissionStatement.Create(Guid.NewGuid(), "COM-E2E-001", employeeId, date, date);
        statement.AddEntry(CommissionEntry.Create(
            Guid.NewGuid(), statement.Id, employeeId, "SalesInvoice",
            invoice.Id, invoiceLine.Id, invoice.Id, invoiceLine.Id, null,
            date, 200m, rule.RatePercent, rule.Id, rule.Code, rule.Name, false));
        statement.AddEntry(CommissionEntry.Create(
            Guid.NewGuid(), statement.Id, employeeId, "SalesReturn",
            salesReturn.Id, salesReturn.Lines.Single().Id, invoice.Id, invoiceLine.Id, salesReturn.Id,
            date, -100m, rule.RatePercent, rule.Id, rule.Code, rule.Name, true));
        statement.MarkCalculated(now, "tester");
        statement.Finalize(now, "manager");

        Assert.That(statement.Status, Is.EqualTo(CommissionStatementStatus.Finalized));
        Assert.That(statement.CommissionBaseAmount, Is.EqualTo(5m));

        var failedJob = OpticalProductionJob.Create(
            Guid.NewGuid(), "OPJ-E2E-001", invoice.Id, invoiceLine.Id, invoice.CustomerId,
            invoiceLine.WarehouseId!.Value, date, date.AddDays(1));
        var material = OpticalProductionMaterial.Create(Guid.NewGuid(), failedJob.Id, Guid.NewGuid(), 1m);
        material.SetIssueCost(20m);
        failedJob.AddMaterial(material);
        failedJob.Release(now);
        failedJob.Start(now);
        failedJob.SetMaterialIssue(Guid.NewGuid());
        failedJob.SubmitQualityControl(now, passed: false, notes: "كسر أثناء التجهيز", isBreakage: true);

        var remake = OpticalProductionJob.Create(
            Guid.NewGuid(), "OPJ-E2E-002", failedJob.SalesInvoiceId, failedJob.SalesInvoiceLineId,
            failedJob.CustomerId, failedJob.WarehouseId, date.AddDays(1), date.AddDays(2),
            "إعادة تصنيع", failedJob.Id, failedJob.RemakeNumber + 1);

        Assert.That(failedJob.Status, Is.EqualTo(OpticalProductionStatus.QualityControlFailed));
        Assert.That(failedJob.LastQcWasBreakage, Is.True);
        Assert.That(remake.RemakeOfJobId, Is.EqualTo(failedJob.Id));
        Assert.That(remake.IsActive, Is.True);
    }

    [Test]
    public void PurchaseReceiptAndReturn_PreserveReturnReservationAndVariance()
    {
        var date = new DateOnly(2026, 10, 3);
        var now = new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
        var receiptLine = PurchaseReceiptLine.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(),
            10m, 0m, 10m, 10m, 0m, 1m, 50m, null, null, null);

        receiptLine.ReserveReturnQuantity(2m);
        var purchaseReturn = PurchaseReturn.Create(
            Guid.NewGuid(), "PRT-E2E-001", receiptLine.PurchaseReceiptId, null,
            Guid.NewGuid(), Guid.NewGuid(), date, date, "supplier return");
        var line = PurchaseReturnLine.Create(
            Guid.NewGuid(), purchaseReturn.Id, 1, receiptLine.Id, null,
            receiptLine.ProductVariantId, 2m, 2m, 50m, 100m, 0m);
        purchaseReturn.AddLine(line);
        purchaseReturn.Confirm(now, "tester");

        line.SetInventoryCostSnapshot(47m);
        purchaseReturn.RefreshInventoryTotals();
        purchaseReturn.MarkPosted(Guid.NewGuid(), now, "tester");

        Assert.That(purchaseReturn.Status, Is.EqualTo(PurchaseReturnStatus.Posted));
        Assert.That(receiptLine.RemainingReturnableQuantity, Is.EqualTo(8m));
        Assert.That(purchaseReturn.ReceiptCostBaseAmount, Is.EqualTo(100m));
        Assert.That(purchaseReturn.InventoryCostBaseAmount, Is.EqualTo(94m));
        Assert.That(purchaseReturn.PurchasePriceVarianceBaseAmount, Is.EqualTo(6m));
    }
}
