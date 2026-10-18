using System.Globalization;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Tests.Purchasing.Infrastructure;

/// <summary>
/// AP-11 integration safety gate. Runs against an explicitly configured populated SQL Server
/// TEST/REPLICA database. No inserts, migrations, mutations or DROP statements are executed.
/// This is an invariant check, NOT a substitute for end-to-end scenario execution.
/// </summary>
[TestFixture, Category("AP11SqlServer"), NonParallelizable]
public sealed class AP11ReadOnlySqlServerAcceptanceTests
{
    private string _connectionString = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_AP11_ACCEPTANCE_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
            Assert.Ignore("Provide OAS_AP11_ACCEPTANCE_SQLSERVER pointing to a TEST/REPLICA DB with AP migrations applied.");

        var cs = new SqlConnectionStringBuilder(configured)
        {
            ApplicationName = "OAS-AP11-ReadOnlyAcceptance",
            ApplicationIntent = ApplicationIntent.ReadOnly
        };
        if (string.IsNullOrWhiteSpace(cs.InitialCatalog) ||
            cs.InitialCatalog.Equals("master", StringComparison.OrdinalIgnoreCase))
            Assert.Fail("AP-11 requires an explicit, non-master test database name.");
        _connectionString = cs.ConnectionString;
        await using var con = await Connect();
        var present = await Scalar(con, "SELECT CASE WHEN OBJECT_ID(N'dbo.tbl_JournalEntries', N'U') IS NULL OR OBJECT_ID(N'dbo.tbl_PurchaseInvoices', N'U') IS NULL THEN 0 ELSE 1 END;");
        Assert.That(present, Is.EqualTo(1), "Apply migrations on the test clone before AP-11 acceptance tests.");
    }

    [Test]
    public async Task Ap11_PostedJournals_AreBalanced_AndHaveAtLeastTwoLines()
    {
        await using var con = await Connect();
        var invalid = await Scalar(con, """
SELECT COUNT_BIG(*) FROM (
    SELECT j.Id
    FROM dbo.tbl_JournalEntries j
    LEFT JOIN dbo.tbl_JournalEntryLines l ON l.JournalEntryId = j.Id
    WHERE j.Status = @posted
    GROUP BY j.Id
    HAVING COUNT(l.Id) < 2 OR ABS(COALESCE(SUM(l.DebitAmount),0) - COALESCE(SUM(l.CreditAmount),0)) > 0.0001
) failures;
""", ("@posted", (byte)JournalEntryStatus.Posted));
        Assert.That(invalid, Is.Zero, "Unbalanced or incomplete posted journals detected; inspect before deployment.");
    }

    [Test]
    public async Task Ap11_OnePostedJournalPerSourceDocument()
    {
        await using var con = await Connect();
        var duplicates = await Scalar(con, """
SELECT COUNT_BIG(*) FROM (
    SELECT SourceModule, SourceDocumentType, SourceDocumentId
    FROM dbo.tbl_JournalEntries
    WHERE Status = @posted AND SourceModule IS NOT NULL
      AND SourceDocumentType IS NOT NULL AND SourceDocumentId IS NOT NULL
    GROUP BY SourceModule, SourceDocumentType, SourceDocumentId HAVING COUNT_BIG(*) > 1
) invalid;
""", ("@posted", (byte)JournalEntryStatus.Posted));
        Assert.That(duplicates, Is.Zero, "Duplicate source document posted more than once.");
    }

    [Test]
    public async Task Ap11_SupplierSubledgerLines_BelongToTheirOwnSupplier()
    {
        await using var con = await Connect();
        var count = await Scalar(con, """
SELECT COUNT_BIG(*)
FROM dbo.tbl_JournalEntryLines l
JOIN dbo.tbl_JournalEntries j ON j.Id=l.JournalEntryId
JOIN dbo.tbl_Suppliers s ON s.AccountId=l.AccountId
WHERE j.Status=@posted AND (l.SupplierId IS NULL OR l.SupplierId<>s.Id);
""", ("@posted", (byte)JournalEntryStatus.Posted));
        Assert.That(count, Is.Zero, "Supplier's subledger account is posted with a different/missing supplier ID.");
    }

    [Test]
    public async Task Ap11_PostedEntries_DoNotWriteDirectlyToNonPostingControlAccounts()
    {
        await using var con = await Connect();
        var invalid = await Scalar(con, """
SELECT COUNT_BIG(*) FROM dbo.tbl_JournalEntryLines l
JOIN dbo.tbl_JournalEntries j ON j.Id=l.JournalEntryId
JOIN dbo.tbl_Accounts a ON a.Id=l.AccountId
WHERE j.Status=@posted AND a.IsControlAccount=1 AND a.IsPostingAccount=0;
""", ("@posted", (byte)JournalEntryStatus.Posted));
        Assert.That(invalid, Is.Zero,
            "Direct journal entries must not post on a non-posting supplier control header.");
    }

    [Test]
    public async Task Ap11_AllSupplierSubledgers_HaveActiveLiabilityControlParent()
    {
        await using var con = await Connect();
        var invalid = await Scalar(con, """
SELECT COUNT_BIG(*) FROM dbo.tbl_Suppliers s
JOIN dbo.tbl_Accounts a ON a.Id=s.AccountId
LEFT JOIN dbo.tbl_Accounts p ON p.Id=a.ParentAccountId
WHERE a.AccountType<>4 OR a.AccountClass<>2 OR a.NormalBalance<>2
   OR a.IsPostingAccount=0 OR a.IsActive=0
   OR p.Id IS NULL OR p.IsActive=0 OR p.IsControlAccount=0
   OR p.AccountType<>3 OR p.AccountClass<>2 OR p.NormalBalance<>2;
""");
        Assert.That(invalid, Is.Zero,
            "AP-10 requires every supplier subledger to belong directly to an active liabilities control parent.");
    }

    [Test]
    public async Task Ap11_PaymentAllocations_DoNotCrossSupplierBoundary()
    {
        await using var con = await Connect();
        var invalid = await Scalar(con, """
SELECT COUNT_BIG(*)
FROM dbo.tbl_PaymentAllocations a
JOIN dbo.tbl_PurchaseInvoices i ON i.Id=a.TargetDocumentId
JOIN dbo.tbl_PaymentVoucherLines v ON v.Id=a.PaymentVoucherLineId
WHERE a.TargetDocumentType=@invoiceType
  AND a.ReversedAtUtc IS NULL
  AND (v.SupplierId IS NULL OR v.SupplierId<>i.SupplierId);
""", ("@invoiceType", (byte)AllocationTargetDocumentType.PurchaseInvoice));
        Assert.That(invalid, Is.Zero, "Cross-supplier payment allocation found.");
    }

    [Test]
    public async Task Ap11_Credits_DoNotCrossSupplierBoundary()
    {
        await using var con = await Connect();
        var invalid = await Scalar(con, """
SELECT COUNT_BIG(*)
FROM dbo.tbl_SupplierCreditApplications ca
JOIN dbo.tbl_PurchaseInvoices i ON i.Id=ca.PurchaseInvoiceId
JOIN dbo.tbl_PurchaseReturns r ON r.Id=ca.PurchaseReturnId
WHERE ca.Status=@active AND (ca.SupplierId<>i.SupplierId OR ca.SupplierId<>r.SupplierId);
""", ("@active", (byte)SupplierCreditApplicationStatus.Active));
        Assert.That(invalid, Is.Zero, "Supplier-credit application crosses invoices/returns from different suppliers.");
    }

    [Test]
    public async Task Ap11_ActiveSettlement_NeverExceedsInvoiceDocumentAmount()
    {
        await using var con = await Connect();
        // TargetDocumentAmount is invoice-currency; AllocatedAmount may be a different
        // payment currency and MUST NOT be used as a fallback in this comparison.
        var invalid = await Scalar(con, """
WITH paid AS (
    SELECT a.TargetDocumentId AS InvoiceId, SUM(a.TargetDocumentAmount) AS Amount
    FROM dbo.tbl_PaymentAllocations a
    WHERE a.TargetDocumentType=@invoiceType AND a.ReversedAtUtc IS NULL
      AND a.TargetDocumentAmount IS NOT NULL
    GROUP BY a.TargetDocumentId
), credited AS (
    SELECT ca.PurchaseInvoiceId AS InvoiceId, SUM(ca.AppliedInvoiceCurrencyAmount) AS Amount
    FROM dbo.tbl_SupplierCreditApplications ca
    WHERE ca.Status=@active GROUP BY ca.PurchaseInvoiceId
)
SELECT COUNT_BIG(*)
FROM dbo.tbl_PurchaseInvoices i
LEFT JOIN paid p ON p.InvoiceId=i.Id
LEFT JOIN credited c ON c.InvoiceId=i.Id
WHERE COALESCE(p.Amount,0) + COALESCE(c.Amount,0) > i.TotalAmount + 0.0001;
""", ("@invoiceType", (byte)AllocationTargetDocumentType.PurchaseInvoice),
        ("@active", (byte)SupplierCreditApplicationStatus.Active));
        Assert.That(invalid, Is.Zero, "Invoice document-currency oversettlement detected.");
    }

    [Test]
    public async Task Ap11_SettlementWithoutInvoiceCurrencySnapshot_IsReportedNotGuessed()
    {
        await using var con = await Connect();
        var missing = await Scalar(con, """
SELECT COUNT_BIG(*) FROM dbo.tbl_PaymentAllocations
WHERE TargetDocumentType=@invoiceType AND ReversedAtUtc IS NULL AND TargetDocumentAmount IS NULL;
""", ("@invoiceType", (byte)AllocationTargetDocumentType.PurchaseInvoice));
        TestContext.Progress.WriteLine($"AP-11: active legacy allocations lacking TargetDocumentAmount: {missing}. Must reconcile individually; not converted from payment currency.");
        if (Environment.GetEnvironmentVariable("OAS_AP11_REQUIRE_FULL_CURRENCY_SNAPSHOTS") == "1")
            Assert.That(missing, Is.Zero, "Unknown historical document currency amounts require review/backfill, never guesses.");
    }

    [Test]
    public async Task Ap11_SchemaHasCriticalIdempotencyAndConcurrencyGuards()
    {
        await using var con = await Connect();
        foreach (var (table, index) in new[]
        {
            ("tbl_JournalEntries", "UX_JournalEntries_SourceDocument"),
            ("tbl_PaymentAllocations", "UX_PaymentAllocations_IdempotencyKey"),
            ("tbl_PurchaseInvoiceReceiptAllocations", "UX_PurchaseInvoiceReceiptAllocations_InvoiceLine_ReceiptLine"),
            ("tbl_PurchasingIntegrationJobs", "UX_PurchasingIntegrationJobs_Receipt_CustomerOrder")
        })
        {
            var found = await Scalar(con, """
SELECT COUNT_BIG(*) FROM sys.indexes i
JOIN sys.tables t ON t.object_id=i.object_id JOIN sys.schemas s ON s.schema_id=t.schema_id
WHERE s.name=N'dbo' AND t.name=@table AND i.name=@index AND i.is_unique=1 AND i.is_disabled=0;
""", ("@table", table), ("@index", index));
            Assert.That(found, Is.EqualTo(1), $"Missing critical unique index dbo.{table}.{index}.");
        }
    }

    [Test]
    public async Task Ap11_NoUntrustedOrDisabledFinanceForeignKeys()
    {
        await using var con = await Connect();
        var invalid = await Scalar(con, """
SELECT COUNT_BIG(*)
FROM sys.foreign_keys fk
JOIN sys.tables t ON t.object_id=fk.parent_object_id
WHERE t.name IN (N'tbl_PurchaseInvoices', N'tbl_PurchaseReceipts',
 N'tbl_PurchaseReturns', N'tbl_PaymentAllocations', N'tbl_JournalEntryLines',
 N'tbl_PurchasingIntegrationJobs') AND (fk.is_disabled=1 OR fk.is_not_trusted=1);
""");
        Assert.That(invalid, Is.Zero, "Finance/purchasing foreign keys must be enabled and trusted.");
    }

    [Test]
    public async Task Ap11_115k_60k_23k_Scenario_RequiresAnIdentifiedInvoice()
    {
        var raw = Environment.GetEnvironmentVariable("OAS_AP11_INVOICE_115K_ID");
        if (!Guid.TryParse(raw, out var id))
            Assert.Ignore("Seed the actual invoice through Purchasing UI/API, then set OAS_AP11_INVOICE_115K_ID (never claim E2E from a fabricated value).");
        await using var con = await Connect();
        var total = await DecimalScalar(con, "SELECT TotalAmount FROM dbo.tbl_PurchaseInvoices WHERE Id=@id AND Status=@posted",
            ("@id", id), ("@posted", (byte)PurchaseInvoiceStatus.Posted));
        Assert.That(total, Is.EqualTo(115000m), "Scenario requires a posted 115000 invoice.");
        var paid = await DecimalScalar(con, """
SELECT COALESCE(SUM(TargetDocumentAmount),0) FROM dbo.tbl_PaymentAllocations
WHERE TargetDocumentId=@id AND TargetDocumentType=@type AND ReversedAtUtc IS NULL;
""", ("@id", id), ("@type", (byte)AllocationTargetDocumentType.PurchaseInvoice));
        var credit = await DecimalScalar(con, """
SELECT COALESCE(SUM(AppliedInvoiceCurrencyAmount),0) FROM dbo.tbl_SupplierCreditApplications
WHERE PurchaseInvoiceId=@id AND Status=@active;
""", ("@id", id), ("@active", (byte)SupplierCreditApplicationStatus.Active));
        Assert.Multiple(() => {
            Assert.That(paid, Is.EqualTo(60000m));
            Assert.That(credit, Is.EqualTo(23000m));
            Assert.That(total - paid - credit, Is.EqualTo(32000m));
        });
    }

    [Test]
    public async Task Ap11_November_Ahmed_SupplierStatement_Opening30k_Closing90k()
    {
        var raw = Environment.GetEnvironmentVariable("OAS_AP11_AHMED_SUPPLIER_ID");
        if (!Guid.TryParse(raw, out var supplierId))
            Assert.Ignore("Create/identify Ahmed supplier with real posted GL opening and November movements, then set OAS_AP11_AHMED_SUPPLIER_ID.");
        await using var con = await Connect();
        var opening = await DecimalScalar(con, """
SELECT COALESCE(SUM(l.CreditAmount-l.DebitAmount),0)
FROM dbo.tbl_Suppliers s JOIN dbo.tbl_JournalEntryLines l ON l.AccountId=s.AccountId
JOIN dbo.tbl_JournalEntries j ON j.Id=l.JournalEntryId
WHERE s.Id=@id AND j.Status=@posted AND j.PostingDate<'2026-11-01';
""", ("@id", supplierId), ("@posted", (byte)JournalEntryStatus.Posted));
        var closing = await DecimalScalar(con, """
SELECT COALESCE(SUM(l.CreditAmount-l.DebitAmount),0)
FROM dbo.tbl_Suppliers s JOIN dbo.tbl_JournalEntryLines l ON l.AccountId=s.AccountId
JOIN dbo.tbl_JournalEntries j ON j.Id=l.JournalEntryId
WHERE s.Id=@id AND j.Status=@posted AND j.PostingDate<='2026-11-30';
""", ("@id", supplierId), ("@posted", (byte)JournalEntryStatus.Posted));
        Assert.Multiple(() => {
            Assert.That(opening, Is.EqualTo(30000m));
            Assert.That(closing, Is.EqualTo(90000m));
        });
        TestContext.Progress.WriteLine($"Ahmed statement acceptance: opening={opening}, movement={closing-opening}, closing={closing} (base currency).");
    }

    [Test]
    public async Task Ap11_MultiSupplierVoucher_ContainsDistinctSupplierLines()
    {
        var raw = Environment.GetEnvironmentVariable("OAS_AP11_MULTISUPPLIER_VOUCHER_ID");
        if (!Guid.TryParse(raw, out var voucherId))
            Assert.Ignore("Post a voucher containing distinct supplier lines for A and B, then set OAS_AP11_MULTISUPPLIER_VOUCHER_ID.");
        await using var con = await Connect();
        var count = await Scalar(con, """
SELECT COUNT_BIG(DISTINCT SupplierId) FROM dbo.tbl_PaymentVoucherLines
WHERE PaymentVoucherId=@id AND SupplierId IS NOT NULL;
""", ("@id", voucherId));
        Assert.That(count, Is.GreaterThanOrEqualTo(2), "A true multi-supplier voucher must contain at least two distinct supplier IDs.");
    }

    [Test]
    public async Task Ap11_100Usd_RealizedFx_UsesAllocationSpecificJournal()
    {
        var raw = Environment.GetEnvironmentVariable("OAS_AP11_FX_ALLOCATION_ID");
        if (!Guid.TryParse(raw, out var allocationId))
            Assert.Ignore("Provide OAS_AP11_FX_ALLOCATION_ID after posting a real 100 USD @1500 invoice and full payment @1600.");
        await using var con = await Connect();
        var difference = await DecimalScalar(con, """
SELECT a.BaseAllocatedAmount - a.TargetBaseAllocatedAmount
FROM dbo.tbl_PaymentAllocations a
JOIN dbo.tbl_PurchaseInvoices i ON i.Id=a.TargetDocumentId
WHERE a.Id=@id AND a.TargetDocumentType=@invoiceType AND a.ReversedAtUtc IS NULL
  AND i.TotalAmount=100 AND i.ExchangeRate=1500
  AND a.TargetDocumentAmount=100 AND a.ExchangeRate=1600;
""", ("@id", allocationId), ("@invoiceType", (byte)AllocationTargetDocumentType.PurchaseInvoice));
        Assert.That(difference, Is.EqualTo(10000m), "100 USD settlement @1600 vs invoice @1500 should realize FX loss 10000.");
        var journalDifference = await DecimalScalar(con, """
SELECT SUM(CASE WHEN l.AccountId = s.ExchangeLossAccountId THEN l.DebitAmount - l.CreditAmount ELSE 0 END)
FROM dbo.tbl_JournalEntries j
JOIN dbo.tbl_JournalEntryLines l ON l.JournalEntryId=j.Id
CROSS JOIN dbo.tbl_AccountingSettings s
WHERE j.SourceModule=N'Accounting' AND j.SourceDocumentType=N'SupplierSettlementFx'
  AND j.SourceDocumentId=@id AND j.Status=@posted;
""", ("@id", allocationId), ("@posted", (byte)JournalEntryStatus.Posted));
        Assert.That(journalDifference, Is.EqualTo(10000m), "Realized FX posting must credit AP and debit FX loss precisely once.");
    }

    private async Task<SqlConnection> Connect()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<long> Scalar(SqlConnection con, string sql, params (string Name, object Value)[] args)
    {
        await using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 60;
        foreach (var (name,value) in args) cmd.Parameters.AddWithValue(name,value);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task<decimal> DecimalScalar(SqlConnection con, string sql, params (string Name, object Value)[] args)
    {
        await using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 60;
        foreach (var (name,value) in args) cmd.Parameters.AddWithValue(name,value);
        var v = await cmd.ExecuteScalarAsync();
        Assert.That(v, Is.Not.Null.And.Not.EqualTo(DBNull.Value), "Missing expected posted scenario document.");
        return Convert.ToDecimal(v, CultureInfo.InvariantCulture);
    }
}
