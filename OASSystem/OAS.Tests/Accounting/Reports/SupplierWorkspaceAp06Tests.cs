using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Application.Accounting.Reports.Queries.GetSupplierWorkspaceDocuments;
using OAS.Contracts.Accounting.Reports;
using OAS.Infrastructure.Accounting.Persistence.Queries;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Accounting.Reports;

[TestFixture]
public sealed class SupplierWorkspaceAp06Tests
{
    private static SupplierWorkspaceDocumentsService Service() =>
        new(new OasDbContext(new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer("Server=localhost;Database=UnusedAp06Test;Integrated Security=True;TrustServerCertificate=True")
            .Options));

    [Test]
    public void Missing_SupplierId_Is_Rejected_Before_Query()
    {
        var query = new SupplierWorkspaceDocumentsFilter(
            Guid.Empty, "PaymentVoucher", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));
        Assert.ThrowsAsync<ArgumentException>(async () => await Service().GetAsync(query));
    }

    [Test]
    public void Inverted_Dates_Are_Rejected_Before_Query()
    {
        var query = new SupplierWorkspaceDocumentsFilter(
            Guid.NewGuid(), "PurchaseInvoice", new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 31));
        Assert.ThrowsAsync<OAS.Application.Common.Exceptions.ConflictException>(
            async () => await Service().GetAsync(query));
    }

    [Test]
    public void Read_Query_Requires_Accounting_And_Purchasing_Permissions()
    {
        var query = new GetSupplierWorkspaceDocumentsQuery(
            new SupplierWorkspaceDocumentsFilter(
                Guid.NewGuid(), "PurchaseInvoice",
                new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)));
        Assert.That(query.RequiredPermissions, Does.Contain("accounting.suppliers.view").Or.Contain("accounting.supplier.view"));
        Assert.That(query.RequiredPermissions, Does.Contain("purchasing.view"));
    }

    [Test]
    public void Document_Row_Supports_Payment_Line_Identity_Without_Mixing_Vouchers()
    {
        var voucher = Guid.NewGuid();
        var lineA = new SupplierWorkspaceDocumentRowDto(voucher, "PaymentVoucher", "PV-1",
            new DateOnly(2026, 1, 1), 3, 20m, 20m, "YER", Guid.NewGuid(), Guid.NewGuid(), 10m);
        var lineB = lineA with { PaymentVoucherLineId = Guid.NewGuid(), BaseAmount = 30m };
        Assert.That(lineA.Id, Is.EqualTo(lineB.Id));
        Assert.That(lineA.PaymentVoucherLineId, Is.Not.EqualTo(lineB.PaymentVoucherLineId));
    }
}
