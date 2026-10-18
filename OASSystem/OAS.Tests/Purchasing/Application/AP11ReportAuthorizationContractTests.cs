using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using NUnit.Framework;
using OAS.API.Accounting.Controllers;
using OAS.API.Printing.Controllers;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Application.Accounting.Reports.Queries.GetSupplierBalances;
using OAS.Application.Accounting.Reports.Queries.GetSupplierStatement;

namespace OAS.Tests.Purchasing.Application;

/// <summary>Structural guard only. Real 401/403 acceptance still needs an authenticated HTTP host.</summary>
[TestFixture, Category("AP11")]
public sealed class AP11ReportAuthorizationContractTests
{
    [Test]
    public void Ap11_SupplierStatementAndBalances_RequireSupplierAndJournalView()
    {
        var statement = new GetSupplierStatementQuery(Guid.NewGuid(),
            new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));
        var balances = new GetSupplierBalancesQuery(new DateOnly(2026, 11, 30));
        Assert.Multiple(() =>
        {
            Assert.That(statement.RequiredPermissions, Does.Contain(AccountingPermissions.Suppliers.View));
            Assert.That(statement.RequiredPermissions, Does.Contain(JournalPermissions.View));
            Assert.That(balances.RequiredPermissions, Does.Contain(AccountingPermissions.Suppliers.View));
            Assert.That(balances.RequiredPermissions, Does.Contain(JournalPermissions.View));
        });
    }

    [Test]
    public void Ap11_SupplierExportAndStatementEndpoints_DemandAuthentication()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(SupplierReportExportsController).GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null,
                "CSV/XLSX exports must not be anonymously accessible.");
            Assert.That(typeof(SupplierApReportsController).GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null,
                "Supplier report reads require authentication.");
            Assert.That(typeof(PrintJobsController).GetMethod(nameof(PrintJobsController.CreateSupplierStatement))!
                .GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null,
                "Supplier report print jobs must not be anonymously accessible.");
        });
    }
}
