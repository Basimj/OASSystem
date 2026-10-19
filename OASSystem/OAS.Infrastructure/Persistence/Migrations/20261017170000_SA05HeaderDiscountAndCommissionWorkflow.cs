using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261017170000_SA05HeaderDiscountAndCommissionWorkflow")]
public sealed class SA05HeaderDiscountAndCommissionWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte>(name: "HeaderDiscountType", schema: "dbo", table: "tbl_CustomerOrders", type: "tinyint", nullable: false, defaultValue: (byte)0);
        migrationBuilder.AddColumn<decimal>(name: "HeaderDiscountValue", schema: "dbo", table: "tbl_CustomerOrders", type: "decimal(19,4)", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "HeaderDiscountAmount", schema: "dbo", table: "tbl_CustomerOrders", type: "decimal(19,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<string>(name: "HeaderDiscountReason", schema: "dbo", table: "tbl_CustomerOrders", type: "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "HeaderDiscountApprovedAtUtc", schema: "dbo", table: "tbl_CustomerOrders", type: "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<string>(name: "HeaderDiscountApprovedBy", schema: "dbo", table: "tbl_CustomerOrders", type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "HeaderDiscountAllocated", schema: "dbo", table: "tbl_CustomerOrderLines", type: "decimal(19,4)", nullable: false, defaultValue: 0m);

        migrationBuilder.AddColumn<byte>(name: "HeaderDiscountType", schema: "dbo", table: "tbl_SalesInvoices", type: "tinyint", nullable: false, defaultValue: (byte)0);
        migrationBuilder.AddColumn<decimal>(name: "HeaderDiscountValue", schema: "dbo", table: "tbl_SalesInvoices", type: "decimal(19,4)", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "HeaderDiscountAmount", schema: "dbo", table: "tbl_SalesInvoices", type: "decimal(19,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>(name: "BaseHeaderDiscountAmount", schema: "dbo", table: "tbl_SalesInvoices", type: "decimal(19,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<string>(name: "HeaderDiscountReason", schema: "dbo", table: "tbl_SalesInvoices", type: "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "HeaderDiscountApprovedAtUtc", schema: "dbo", table: "tbl_SalesInvoices", type: "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<string>(name: "HeaderDiscountApprovedBy", schema: "dbo", table: "tbl_SalesInvoices", type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "HeaderDiscountAllocated", schema: "dbo", table: "tbl_SalesInvoiceLines", type: "decimal(19,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>(name: "BaseHeaderDiscountAllocated", schema: "dbo", table: "tbl_SalesInvoiceLines", type: "decimal(19,4)", nullable: false, defaultValue: 0m);

        migrationBuilder.AddColumn<Guid>(name: "ProductVariantId", schema: "dbo", table: "tbl_CommissionRules", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "ProductCategoryId", schema: "dbo", table: "tbl_CommissionRules", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<byte>(name: "SaleType", schema: "dbo", table: "tbl_CommissionRules", type: "tinyint", nullable: true);
        migrationBuilder.AddColumn<int>(name: "Priority", schema: "dbo", table: "tbl_CommissionRules", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<byte>(name: "BasisKind", schema: "dbo", table: "tbl_CommissionRules", type: "tinyint", nullable: false, defaultValue: (byte)1);
        migrationBuilder.AddColumn<byte>(name: "BasisKindSnapshot", schema: "dbo", table: "tbl_CommissionEntries", type: "tinyint", nullable: false, defaultValue: (byte)1);

        migrationBuilder.AddColumn<DateTimeOffset>(name: "ReviewedAtUtc", schema: "dbo", table: "tbl_CommissionStatements", type: "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ReviewedBy", schema: "dbo", table: "tbl_CommissionStatements", type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "ApprovedAtUtc", schema: "dbo", table: "tbl_CommissionStatements", type: "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ApprovedBy", schema: "dbo", table: "tbl_CommissionStatements", type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "SettledAtUtc", schema: "dbo", table: "tbl_CommissionStatements", type: "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SettledBy", schema: "dbo", table: "tbl_CommissionStatements", type: "nvarchar(64)", maxLength: 64, nullable: true);

        migrationBuilder.DropIndex(name: "IX_CommissionRules_Resolution", schema: "dbo", table: "tbl_CommissionRules");
        migrationBuilder.CreateIndex(name: "IX_CommissionRules_Resolution", schema: "dbo", table: "tbl_CommissionRules", columns: new[] { "EmployeeId", "ProductVariantId", "ProductCategoryId", "SaleType", "Priority", "EffectiveFrom", "IsActive" });
        migrationBuilder.CreateIndex(name: "IX_CommissionRules_ProductVariantId", schema: "dbo", table: "tbl_CommissionRules", column: "ProductVariantId");
        migrationBuilder.CreateIndex(name: "IX_CommissionRules_ProductCategoryId", schema: "dbo", table: "tbl_CommissionRules", column: "ProductCategoryId");
        migrationBuilder.AddForeignKey(name: "FK_CommissionRules_ProductVariants_ProductVariantId", schema: "dbo", table: "tbl_CommissionRules", column: "ProductVariantId", principalSchema: "dbo", principalTable: "tbl_ProductVariants", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_CommissionRules_ProductCategories_ProductCategoryId", schema: "dbo", table: "tbl_CommissionRules", column: "ProductCategoryId", principalSchema: "dbo", principalTable: "tbl_ProductCategories", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_CustomerOrders_Totals_NonNegative') ALTER TABLE [dbo].[tbl_CustomerOrders] DROP CONSTRAINT [CK_CustomerOrders_Totals_NonNegative];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_CustomerOrderLines_DiscountWithinGross') ALTER TABLE [dbo].[tbl_CustomerOrderLines] DROP CONSTRAINT [CK_CustomerOrderLines_DiscountWithinGross];
ALTER TABLE [dbo].[tbl_CustomerOrders] ADD CONSTRAINT [CK_CustomerOrders_Totals_NonNegative] CHECK ([Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [HeaderDiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0);
ALTER TABLE [dbo].[tbl_CustomerOrderLines] ADD CONSTRAINT [CK_CustomerOrderLines_HeaderDiscount_NonNegative] CHECK ([HeaderDiscountAllocated] >= 0);
ALTER TABLE [dbo].[tbl_CustomerOrderLines] ADD CONSTRAINT [CK_CustomerOrderLines_DiscountsWithinGross] CHECK (([DiscountAmount] + [HeaderDiscountAllocated]) <= ([Quantity] * [ActualUnitPrice]));
""");

        migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesInvoices_Totals_NonNegative') ALTER TABLE [dbo].[tbl_SalesInvoices] DROP CONSTRAINT [CK_SalesInvoices_Totals_NonNegative];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesInvoices_BaseTotals_NonNegative') ALTER TABLE [dbo].[tbl_SalesInvoices] DROP CONSTRAINT [CK_SalesInvoices_BaseTotals_NonNegative];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesInvoiceLines_DiscountWithinGross') ALTER TABLE [dbo].[tbl_SalesInvoiceLines] DROP CONSTRAINT [CK_SalesInvoiceLines_DiscountWithinGross];
ALTER TABLE [dbo].[tbl_SalesInvoices] ADD CONSTRAINT [CK_SalesInvoices_Totals_NonNegative] CHECK ([Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [HeaderDiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0);
ALTER TABLE [dbo].[tbl_SalesInvoices] ADD CONSTRAINT [CK_SalesInvoices_BaseTotals_NonNegative] CHECK ([BaseSubtotal] >= 0 AND [BaseDiscountAmount] >= 0 AND [BaseHeaderDiscountAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseTotalAmount] >= 0);
ALTER TABLE [dbo].[tbl_SalesInvoiceLines] ADD CONSTRAINT [CK_SalesInvoiceLines_HeaderDiscount_NonNegative] CHECK ([HeaderDiscountAllocated] >= 0 AND [BaseHeaderDiscountAllocated] >= 0);
ALTER TABLE [dbo].[tbl_SalesInvoiceLines] ADD CONSTRAINT [CK_SalesInvoiceLines_DiscountsWithinGross] CHECK (([DiscountAmount] + [HeaderDiscountAllocated]) <= ([Quantity] * [ActualUnitPrice]));
ALTER TABLE [dbo].[tbl_CommissionRules] ADD CONSTRAINT [CK_CommissionRules_Priority] CHECK ([Priority] >= 0 AND [Priority] <= 10000);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_CommissionRules_ProductVariants_ProductVariantId", schema: "dbo", table: "tbl_CommissionRules");
        migrationBuilder.DropForeignKey(name: "FK_CommissionRules_ProductCategories_ProductCategoryId", schema: "dbo", table: "tbl_CommissionRules");
        migrationBuilder.DropIndex(name: "IX_CommissionRules_Resolution", schema: "dbo", table: "tbl_CommissionRules");
        migrationBuilder.DropIndex(name: "IX_CommissionRules_ProductVariantId", schema: "dbo", table: "tbl_CommissionRules");
        migrationBuilder.DropIndex(name: "IX_CommissionRules_ProductCategoryId", schema: "dbo", table: "tbl_CommissionRules");

        // Drop ALL SA-05 CHECK constraints referring to header discount columns before
        // dropping those columns. SQL Server otherwise rejects ALTER TABLE DROP COLUMN.
        migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_CustomerOrders_Totals_NonNegative' AND [parent_object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerOrders]'))
    ALTER TABLE [dbo].[tbl_CustomerOrders] DROP CONSTRAINT [CK_CustomerOrders_Totals_NonNegative];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesInvoices_Totals_NonNegative' AND [parent_object_id] = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]'))
    ALTER TABLE [dbo].[tbl_SalesInvoices] DROP CONSTRAINT [CK_SalesInvoices_Totals_NonNegative];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesInvoices_BaseTotals_NonNegative' AND [parent_object_id] = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]'))
    ALTER TABLE [dbo].[tbl_SalesInvoices] DROP CONSTRAINT [CK_SalesInvoices_BaseTotals_NonNegative];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_CommissionRules_Priority') ALTER TABLE [dbo].[tbl_CommissionRules] DROP CONSTRAINT [CK_CommissionRules_Priority];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesInvoiceLines_DiscountsWithinGross') ALTER TABLE [dbo].[tbl_SalesInvoiceLines] DROP CONSTRAINT [CK_SalesInvoiceLines_DiscountsWithinGross];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesInvoiceLines_HeaderDiscount_NonNegative') ALTER TABLE [dbo].[tbl_SalesInvoiceLines] DROP CONSTRAINT [CK_SalesInvoiceLines_HeaderDiscount_NonNegative];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_CustomerOrderLines_DiscountsWithinGross') ALTER TABLE [dbo].[tbl_CustomerOrderLines] DROP CONSTRAINT [CK_CustomerOrderLines_DiscountsWithinGross];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_CustomerOrderLines_HeaderDiscount_NonNegative') ALTER TABLE [dbo].[tbl_CustomerOrderLines] DROP CONSTRAINT [CK_CustomerOrderLines_HeaderDiscount_NonNegative];
""");

        migrationBuilder.DropColumn(name: "ReviewedAtUtc", schema: "dbo", table: "tbl_CommissionStatements");
        migrationBuilder.DropColumn(name: "ReviewedBy", schema: "dbo", table: "tbl_CommissionStatements");
        migrationBuilder.DropColumn(name: "ApprovedAtUtc", schema: "dbo", table: "tbl_CommissionStatements");
        migrationBuilder.DropColumn(name: "ApprovedBy", schema: "dbo", table: "tbl_CommissionStatements");
        migrationBuilder.DropColumn(name: "SettledAtUtc", schema: "dbo", table: "tbl_CommissionStatements");
        migrationBuilder.DropColumn(name: "SettledBy", schema: "dbo", table: "tbl_CommissionStatements");
        migrationBuilder.DropColumn(name: "BasisKindSnapshot", schema: "dbo", table: "tbl_CommissionEntries");
        migrationBuilder.DropColumn(name: "ProductVariantId", schema: "dbo", table: "tbl_CommissionRules");
        migrationBuilder.DropColumn(name: "ProductCategoryId", schema: "dbo", table: "tbl_CommissionRules");
        migrationBuilder.DropColumn(name: "SaleType", schema: "dbo", table: "tbl_CommissionRules");
        migrationBuilder.DropColumn(name: "Priority", schema: "dbo", table: "tbl_CommissionRules");
        migrationBuilder.DropColumn(name: "BasisKind", schema: "dbo", table: "tbl_CommissionRules");

        migrationBuilder.DropColumn(name: "HeaderDiscountAllocated", schema: "dbo", table: "tbl_CustomerOrderLines");
        migrationBuilder.DropColumn(name: "HeaderDiscountType", schema: "dbo", table: "tbl_CustomerOrders");
        migrationBuilder.DropColumn(name: "HeaderDiscountValue", schema: "dbo", table: "tbl_CustomerOrders");
        migrationBuilder.DropColumn(name: "HeaderDiscountAmount", schema: "dbo", table: "tbl_CustomerOrders");
        migrationBuilder.DropColumn(name: "HeaderDiscountReason", schema: "dbo", table: "tbl_CustomerOrders");
        migrationBuilder.DropColumn(name: "HeaderDiscountApprovedAtUtc", schema: "dbo", table: "tbl_CustomerOrders");
        migrationBuilder.DropColumn(name: "HeaderDiscountApprovedBy", schema: "dbo", table: "tbl_CustomerOrders");

        migrationBuilder.DropColumn(name: "HeaderDiscountAllocated", schema: "dbo", table: "tbl_SalesInvoiceLines");
        migrationBuilder.DropColumn(name: "BaseHeaderDiscountAllocated", schema: "dbo", table: "tbl_SalesInvoiceLines");
        migrationBuilder.DropColumn(name: "HeaderDiscountType", schema: "dbo", table: "tbl_SalesInvoices");
        migrationBuilder.DropColumn(name: "HeaderDiscountValue", schema: "dbo", table: "tbl_SalesInvoices");
        migrationBuilder.DropColumn(name: "HeaderDiscountAmount", schema: "dbo", table: "tbl_SalesInvoices");
        migrationBuilder.DropColumn(name: "BaseHeaderDiscountAmount", schema: "dbo", table: "tbl_SalesInvoices");
        migrationBuilder.DropColumn(name: "HeaderDiscountReason", schema: "dbo", table: "tbl_SalesInvoices");
        migrationBuilder.DropColumn(name: "HeaderDiscountApprovedAtUtc", schema: "dbo", table: "tbl_SalesInvoices");
        migrationBuilder.DropColumn(name: "HeaderDiscountApprovedBy", schema: "dbo", table: "tbl_SalesInvoices");

        // Restore the CHECK constraints which were present before SA-05. They MUST be
        // added only after the header discount columns have been dropped.
        migrationBuilder.Sql("""
ALTER TABLE [dbo].[tbl_CustomerOrders] WITH CHECK ADD CONSTRAINT [CK_CustomerOrders_Totals_NonNegative]
    CHECK ([Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0);
ALTER TABLE [dbo].[tbl_SalesInvoices] WITH CHECK ADD CONSTRAINT [CK_SalesInvoices_Totals_NonNegative]
    CHECK ([Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0);
ALTER TABLE [dbo].[tbl_SalesInvoices] WITH CHECK ADD CONSTRAINT [CK_SalesInvoices_BaseTotals_NonNegative]
    CHECK ([BaseSubtotal] >= 0 AND [BaseDiscountAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseTotalAmount] >= 0);
ALTER TABLE [dbo].[tbl_CustomerOrderLines] WITH CHECK ADD CONSTRAINT [CK_CustomerOrderLines_DiscountWithinGross]
    CHECK ([DiscountAmount] <= ([Quantity] * [ActualUnitPrice]));
ALTER TABLE [dbo].[tbl_SalesInvoiceLines] WITH CHECK ADD CONSTRAINT [CK_SalesInvoiceLines_DiscountWithinGross]
    CHECK ([DiscountAmount] <= ([Quantity] * [ActualUnitPrice]));
""");

        migrationBuilder.CreateIndex(name: "IX_CommissionRules_Resolution", schema: "dbo", table: "tbl_CommissionRules", columns: new[] { "EmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive" });
    }
}
