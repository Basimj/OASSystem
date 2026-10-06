using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261005170000_CompleteSalesWorkflowInfrastructure")]
public sealed class CompleteSalesWorkflowInfrastructure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

/* ============================================================
   1) Sales payment plan snapshots (Expand -> Backfill -> Contract)
   ============================================================ */
IF COL_LENGTH(N'dbo.tbl_CustomerOrders', N'PaymentPlan') IS NULL
    ALTER TABLE [dbo].[tbl_CustomerOrders] ADD [PaymentPlan] tinyint NULL;

/* Dynamic SQL is intentional here. SQL Server binds column references for the whole
   batch before executing the guarded ALTER TABLE above, so a direct UPDATE would fail
   with "Invalid column name 'PaymentPlan'" on databases that do not have the column yet. */
EXEC(N'UPDATE [dbo].[tbl_CustomerOrders]
       SET [PaymentPlan] = CASE WHEN [PaymentTermType] = 2 THEN 4 ELSE 1 END
       WHERE [PaymentPlan] IS NULL;');

IF EXISTS
(
    SELECT 1 FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerOrders]')
      AND [name] = N'PaymentPlan' AND [is_nullable] = 1
)
    EXEC(N'ALTER TABLE [dbo].[tbl_CustomerOrders]
           ALTER COLUMN [PaymentPlan] tinyint NOT NULL;');

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_CustomerOrders_PaymentPlan'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerOrders]')
)
    EXEC(N'CREATE INDEX [IX_CustomerOrders_PaymentPlan]
           ON [dbo].[tbl_CustomerOrders]([PaymentPlan]);');

IF COL_LENGTH(N'dbo.tbl_SalesInvoices', N'PaymentPlan') IS NULL
    ALTER TABLE [dbo].[tbl_SalesInvoices] ADD [PaymentPlan] tinyint NULL;

EXEC(N'UPDATE [dbo].[tbl_SalesInvoices]
       SET [PaymentPlan] = CASE WHEN [PaymentTermType] = 2 THEN 4 ELSE 1 END
       WHERE [PaymentPlan] IS NULL;');

IF EXISTS
(
    SELECT 1 FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]')
      AND [name] = N'PaymentPlan' AND [is_nullable] = 1
)
    EXEC(N'ALTER TABLE [dbo].[tbl_SalesInvoices]
           ALTER COLUMN [PaymentPlan] tinyint NOT NULL;');

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_SalesInvoices_PaymentPlan'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]')
)
    EXEC(N'CREATE INDEX [IX_SalesInvoices_PaymentPlan]
           ON [dbo].[tbl_SalesInvoices]([PaymentPlan]);');

/* ============================================================
   2) Manual optical measurements on invoice snapshot
   Stored-prescription rows keep their FK; manual rows may be NULL.
   ============================================================ */
IF COL_LENGTH(N'dbo.tbl_SalesInvoiceLinePrescriptionSnapshots', N'PrescriptionRevisionId') IS NOT NULL
AND EXISTS
(
    SELECT 1 FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLinePrescriptionSnapshots]')
      AND [name] = N'PrescriptionRevisionId' AND [is_nullable] = 0
)
    ALTER TABLE [dbo].[tbl_SalesInvoiceLinePrescriptionSnapshots]
        ALTER COLUMN [PrescriptionRevisionId] uniqueidentifier NULL;

/* ============================================================
   3) PaymentAllocation typed source hardening
   Backfill legacy generic pointers where the relationship is deterministic.
   ============================================================ */
IF OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]', N'U') IS NOT NULL
BEGIN
    /* Legacy ReceiptVoucherLine id already stored in PaymentSourceId. */
    UPDATE a
       SET [ReceiptVoucherLineId] = a.[PaymentSourceId]
    FROM [dbo].[tbl_PaymentAllocations] a
    WHERE a.[PaymentSourceType] = 1
      AND a.[ReceiptVoucherLineId] IS NULL
      AND a.[PaymentVoucherLineId] IS NULL
      AND a.[CustomerAdvanceApplicationId] IS NULL
      AND EXISTS (SELECT 1 FROM [dbo].[tbl_ReceiptVoucherLines] l WHERE l.[Id] = a.[PaymentSourceId]);

    /* Legacy ReceiptVoucher header id: migrate only when one source line is unambiguous. */
    UPDATE a
       SET [ReceiptVoucherLineId] =
       (
           SELECT TOP (1) l.[Id]
           FROM [dbo].[tbl_ReceiptVoucherLines] l
           WHERE l.[ReceiptVoucherId] = a.[PaymentSourceId]
           ORDER BY l.[LineNumber], l.[Id]
       )
    FROM [dbo].[tbl_PaymentAllocations] a
    WHERE a.[PaymentSourceType] = 1
      AND a.[ReceiptVoucherLineId] IS NULL
      AND a.[PaymentVoucherLineId] IS NULL
      AND a.[CustomerAdvanceApplicationId] IS NULL
      AND 1 =
      (
          SELECT COUNT(*)
          FROM [dbo].[tbl_ReceiptVoucherLines] l
          WHERE l.[ReceiptVoucherId] = a.[PaymentSourceId]
      );

    /* Legacy PaymentVoucherLine id already stored in PaymentSourceId. */
    UPDATE a
       SET [PaymentVoucherLineId] = a.[PaymentSourceId]
    FROM [dbo].[tbl_PaymentAllocations] a
    WHERE a.[PaymentSourceType] = 2
      AND a.[ReceiptVoucherLineId] IS NULL
      AND a.[PaymentVoucherLineId] IS NULL
      AND a.[CustomerAdvanceApplicationId] IS NULL
      AND EXISTS (SELECT 1 FROM [dbo].[tbl_PaymentVoucherLines] l WHERE l.[Id] = a.[PaymentSourceId]);

    /* Legacy PaymentVoucher header id: migrate only when one source line is unambiguous. */
    UPDATE a
       SET [PaymentVoucherLineId] =
       (
           SELECT TOP (1) l.[Id]
           FROM [dbo].[tbl_PaymentVoucherLines] l
           WHERE l.[PaymentVoucherId] = a.[PaymentSourceId]
           ORDER BY l.[LineNumber], l.[Id]
       )
    FROM [dbo].[tbl_PaymentAllocations] a
    WHERE a.[PaymentSourceType] = 2
      AND a.[ReceiptVoucherLineId] IS NULL
      AND a.[PaymentVoucherLineId] IS NULL
      AND a.[CustomerAdvanceApplicationId] IS NULL
      AND 1 =
      (
          SELECT COUNT(*)
          FROM [dbo].[tbl_PaymentVoucherLines] l
          WHERE l.[PaymentVoucherId] = a.[PaymentSourceId]
      );

    /* CustomerAdvanceApplication id already stored in the legacy pointer. */
    UPDATE a
       SET [CustomerAdvanceApplicationId] = a.[PaymentSourceId]
    FROM [dbo].[tbl_PaymentAllocations] a
    WHERE a.[PaymentSourceType] = 3
      AND a.[ReceiptVoucherLineId] IS NULL
      AND a.[PaymentVoucherLineId] IS NULL
      AND a.[CustomerAdvanceApplicationId] IS NULL
      AND EXISTS (SELECT 1 FROM [dbo].[tbl_CustomerAdvanceApplications] x WHERE x.[Id] = a.[PaymentSourceId]);

    /* Older CustomerAdvance id: resolve by advance + target SalesInvoice when unique. */
    UPDATE a
       SET [CustomerAdvanceApplicationId] =
       (
           SELECT TOP (1) caa.[Id]
           FROM [dbo].[tbl_CustomerAdvanceApplications] caa
           WHERE caa.[CustomerAdvanceId] = a.[PaymentSourceId]
             AND caa.[SalesInvoiceId] = a.[TargetDocumentId]
           ORDER BY caa.[AppliedAtUtc], caa.[Id]
       )
    FROM [dbo].[tbl_PaymentAllocations] a
    WHERE a.[PaymentSourceType] = 3
      AND a.[ReceiptVoucherLineId] IS NULL
      AND a.[PaymentVoucherLineId] IS NULL
      AND a.[CustomerAdvanceApplicationId] IS NULL
      AND 1 =
      (
          SELECT COUNT(*)
          FROM [dbo].[tbl_CustomerAdvanceApplications] caa
          WHERE caa.[CustomerAdvanceId] = a.[PaymentSourceId]
            AND caa.[SalesInvoiceId] = a.[TargetDocumentId]
      );

    IF EXISTS
    (
        SELECT 1
        FROM [dbo].[tbl_PaymentAllocations]
        WHERE (CASE WHEN [ReceiptVoucherLineId] IS NULL THEN 0 ELSE 1 END
             + CASE WHEN [PaymentVoucherLineId] IS NULL THEN 0 ELSE 1 END
             + CASE WHEN [CustomerAdvanceApplicationId] IS NULL THEN 0 ELSE 1 END) <> 1
    )
        THROW 51001, 'PaymentAllocation legacy source backfill is ambiguous. Resolve the remaining legacy source rows before applying CompleteSalesWorkflowInfrastructure.', 1;

    IF EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE [name] = N'CK_PaymentAllocations_TypedSource'
          AND [parent_object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]')
    )
        ALTER TABLE [dbo].[tbl_PaymentAllocations]
            DROP CONSTRAINT [CK_PaymentAllocations_TypedSource];

    ALTER TABLE [dbo].[tbl_PaymentAllocations] WITH CHECK
        ADD CONSTRAINT [CK_PaymentAllocations_TypedSource]
        CHECK
        (
            (CASE WHEN [ReceiptVoucherLineId] IS NULL THEN 0 ELSE 1 END
           + CASE WHEN [PaymentVoucherLineId] IS NULL THEN 0 ELSE 1 END
           + CASE WHEN [CustomerAdvanceApplicationId] IS NULL THEN 0 ELSE 1 END) = 1
        );
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

/* Restore the expand-compatible typed source constraint. */
IF EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE [name] = N'CK_PaymentAllocations_TypedSource'
      AND [parent_object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]')
)
    ALTER TABLE [dbo].[tbl_PaymentAllocations]
        DROP CONSTRAINT [CK_PaymentAllocations_TypedSource];

IF OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]', N'U') IS NOT NULL
    ALTER TABLE [dbo].[tbl_PaymentAllocations] WITH CHECK
        ADD CONSTRAINT [CK_PaymentAllocations_TypedSource]
        CHECK
        (
            (CASE WHEN [ReceiptVoucherLineId] IS NULL THEN 0 ELSE 1 END
           + CASE WHEN [PaymentVoucherLineId] IS NULL THEN 0 ELSE 1 END
           + CASE WHEN [CustomerAdvanceApplicationId] IS NULL THEN 0 ELSE 1 END) <= 1
        );

IF EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_SalesInvoices_PaymentPlan'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]')
)
    DROP INDEX [IX_SalesInvoices_PaymentPlan] ON [dbo].[tbl_SalesInvoices];

IF COL_LENGTH(N'dbo.tbl_SalesInvoices', N'PaymentPlan') IS NOT NULL
    ALTER TABLE [dbo].[tbl_SalesInvoices] DROP COLUMN [PaymentPlan];

IF EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_CustomerOrders_PaymentPlan'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerOrders]')
)
    DROP INDEX [IX_CustomerOrders_PaymentPlan] ON [dbo].[tbl_CustomerOrders];

IF COL_LENGTH(N'dbo.tbl_CustomerOrders', N'PaymentPlan') IS NOT NULL
    ALTER TABLE [dbo].[tbl_CustomerOrders] DROP COLUMN [PaymentPlan];

/* Nullable prescription revision is intentionally retained on rollback when manual
   invoice snapshots exist; forcing NOT NULL could destroy or invalidate those rows. */
""");
    }
}
