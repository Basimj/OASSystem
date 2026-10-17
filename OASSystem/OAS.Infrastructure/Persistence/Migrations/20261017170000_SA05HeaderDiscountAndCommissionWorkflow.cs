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
        migrationBuilder.AddColumn<byte>("HeaderDiscountType", "dbo", "tbl_CustomerOrders", "tinyint", nullable: false, defaultValue: (byte)0);
        migrationBuilder.AddColumn<decimal>("HeaderDiscountValue", "dbo", "tbl_CustomerOrders", "decimal(19,4)", nullable: true);
        migrationBuilder.AddColumn<decimal>("HeaderDiscountAmount", "dbo", "tbl_CustomerOrders", "decimal(19,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<string>("HeaderDiscountReason", "dbo", "tbl_CustomerOrders", "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>("HeaderDiscountApprovedAtUtc", "dbo", "tbl_CustomerOrders", "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<string>("HeaderDiscountApprovedBy", "dbo", "tbl_CustomerOrders", "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<decimal>("HeaderDiscountAllocated", "dbo", "tbl_CustomerOrderLines", "decimal(19,4)", nullable: false, defaultValue: 0m);

        migrationBuilder.AddColumn<byte>("HeaderDiscountType", "dbo", "tbl_SalesInvoices", "tinyint", nullable: false, defaultValue: (byte)0);
        migrationBuilder.AddColumn<decimal>("HeaderDiscountValue", "dbo", "tbl_SalesInvoices", "decimal(19,4)", nullable: true);
        migrationBuilder.AddColumn<decimal>("HeaderDiscountAmount", "dbo", "tbl_SalesInvoices", "decimal(19,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>("BaseHeaderDiscountAmount", "dbo", "tbl_SalesInvoices", "decimal(19,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<string>("HeaderDiscountReason", "dbo", "tbl_SalesInvoices", "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>("HeaderDiscountApprovedAtUtc", "dbo", "tbl_SalesInvoices", "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<string>("HeaderDiscountApprovedBy", "dbo", "tbl_SalesInvoices", "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<decimal>("HeaderDiscountAllocated", "dbo", "tbl_SalesInvoiceLines", "decimal(19,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>("BaseHeaderDiscountAllocated", "dbo", "tbl_SalesInvoiceLines", "decimal(19,4)", nullable: false, defaultValue: 0m);

        migrationBuilder.AddColumn<Guid>("ProductVariantId", "dbo", "tbl_CommissionRules", "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<Guid>("ProductCategoryId", "dbo", "tbl_CommissionRules", "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<byte>("SaleType", "dbo", "tbl_CommissionRules", "tinyint", nullable: true);
        migrationBuilder.AddColumn<int>("Priority", "dbo", "tbl_CommissionRules", "int", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<byte>("BasisKind", "dbo", "tbl_CommissionRules", "tinyint", nullable: false, defaultValue: (byte)1);
        migrationBuilder.AddColumn<byte>("BasisKindSnapshot", "dbo", "tbl_CommissionEntries", "tinyint", nullable: false, defaultValue: (byte)1);

        migrationBuilder.AddColumn<DateTimeOffset>("ReviewedAtUtc", "dbo", "tbl_CommissionStatements", "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<string>("ReviewedBy", "dbo", "tbl_CommissionStatements", "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>("ApprovedAtUtc", "dbo", "tbl_CommissionStatements", "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<string>("ApprovedBy", "dbo", "tbl_CommissionStatements", "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>("SettledAtUtc", "dbo", "tbl_CommissionStatements", "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<string>("SettledBy", "dbo", "tbl_CommissionStatements", "nvarchar(64)", maxLength: 64, nullable: true);

        migrationBuilder.DropIndex("IX_CommissionRules_Resolution", "dbo", "tbl_CommissionRules");
        migrationBuilder.CreateIndex("IX_CommissionRules_Resolution", "tbl_CommissionRules",
            new[] { "EmployeeId", "ProductVariantId", "ProductCategoryId", "SaleType", "Priority", "EffectiveFrom", "IsActive" }, schema: "dbo");
        migrationBuilder.CreateIndex("IX_CommissionRules_ProductVariantId", "tbl_CommissionRules", "ProductVariantId", schema: "dbo");
        migrationBuilder.CreateIndex("IX_CommissionRules_ProductCategoryId", "tbl_CommissionRules", "ProductCategoryId", schema: "dbo");
        migrationBuilder.AddForeignKey("FK_CommissionRules_ProductVariants_ProductVariantId", "tbl_CommissionRules", "ProductVariantId",
            "tbl_ProductVariants", "Id", principalSchema: "dbo", schema: "dbo", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("FK_CommissionRules_ProductCategories_ProductCategoryId", "tbl_CommissionRules", "ProductCategoryId",
            "tbl_ProductCategories", "Id", principalSchema: "dbo", schema: "dbo", onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_CustomerOrders_Totals_NonNegative') ALTER TABLE [dbo].[tbl_CustomerOrders] DROP CONSTRAINT [CK_CustomerOrders_Totals_NonNegative];
ALTER TABLE [dbo].[tbl_CustomerOrders] ADD CONSTRAINT [CK_CustomerOrders_Totals_NonNegative] CHECK ([Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [HeaderDiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0);
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_CustomerOrderLines_DiscountWithinGross') ALTER TABLE [dbo].[tbl_CustomerOrderLines] DROP CONSTRAINT [CK_CustomerOrderLines_DiscountWithinGross];
ALTER TABLE [dbo].[tbl_CustomerOrderLines] ADD CONSTRAINT [CK_CustomerOrderLines_HeaderDiscount_NonNegative] CHECK ([HeaderDiscountAllocated] >= 0);
ALTER TABLE [dbo].[tbl_CustomerOrderLines] ADD CONSTRAINT [CK_CustomerOrderLines_DiscountsWithinGross] CHECK (([DiscountAmount] + [HeaderDiscountAllocated]) <= ([Quantity] * [ActualUnitPrice]));
""");

        migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesInvoices_Totals_NonNegative') ALTER TABLE [dbo].[tbl_SalesInvoices] DROP CONSTRAINT [CK_SalesInvoices_Totals_NonNegative];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesInvoices_BaseTotals_NonNegative') ALTER TABLE [dbo].[tbl_SalesInvoices] DROP CONSTRAINT [CK_SalesInvoices_BaseTotals_NonNegative];
ALTER TABLE [dbo].[tbl_SalesInvoices] ADD CONSTRAINT [CK_SalesInvoices_Totals_NonNegative] CHECK ([Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [HeaderDiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0);
ALTER TABLE [dbo].[tbl_SalesInvoices] ADD CONSTRAINT [CK_SalesInvoices_BaseTotals_NonNegative] CHECK ([BaseSubtotal] >= 0 AND [BaseDiscountAmount] >= 0 AND [BaseHeaderDiscountAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseTotalAmount] >= 0);
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesInvoiceLines_DiscountWithinGross') ALTER TABLE [dbo].[tbl_SalesInvoiceLines] DROP CONSTRAINT [CK_SalesInvoiceLines_DiscountWithinGross];
ALTER TABLE [dbo].[tbl_SalesInvoiceLines] ADD CONSTRAINT [CK_SalesInvoiceLines_HeaderDiscount_NonNegative] CHECK ([HeaderDiscountAllocated] >= 0 AND [BaseHeaderDiscountAllocated] >= 0);
ALTER TABLE [dbo].[tbl_SalesInvoiceLines] ADD CONSTRAINT [CK_SalesInvoiceLines_DiscountsWithinGross] CHECK (([DiscountAmount] + [HeaderDiscountAllocated]) <= ([Quantity] * [ActualUnitPrice]));
ALTER TABLE [dbo].[tbl_CommissionRules] ADD CONSTRAINT [CK_CommissionRules_Priority] CHECK ([Priority] >= 0 AND [Priority] <= 10000);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_CommissionRules_ProductVariants_ProductVariantId", "tbl_CommissionRules", schema: "dbo");
        migrationBuilder.DropForeignKey("FK_CommissionRules_ProductCategories_ProductCategoryId", "tbl_CommissionRules", schema: "dbo");
        migrationBuilder.DropIndex("IX_CommissionRules_Resolution", "tbl_CommissionRules", schema: "dbo");
        migrationBuilder.DropIndex("IX_CommissionRules_ProductVariantId", "tbl_CommissionRules", schema: "dbo");
        migrationBuilder.DropIndex("IX_CommissionRules_ProductCategoryId", "tbl_CommissionRules", schema: "dbo");

        migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_CommissionRules_Priority') ALTER TABLE [dbo].[tbl_CommissionRules] DROP CONSTRAINT [CK_CommissionRules_Priority];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesInvoiceLines_DiscountsWithinGross') ALTER TABLE [dbo].[tbl_SalesInvoiceLines] DROP CONSTRAINT [CK_SalesInvoiceLines_DiscountsWithinGross];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesInvoiceLines_HeaderDiscount_NonNegative') ALTER TABLE [dbo].[tbl_SalesInvoiceLines] DROP CONSTRAINT [CK_SalesInvoiceLines_HeaderDiscount_NonNegative];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_CustomerOrderLines_DiscountsWithinGross') ALTER TABLE [dbo].[tbl_CustomerOrderLines] DROP CONSTRAINT [CK_CustomerOrderLines_DiscountsWithinGross];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_CustomerOrderLines_HeaderDiscount_NonNegative') ALTER TABLE [dbo].[tbl_CustomerOrderLines] DROP CONSTRAINT [CK_CustomerOrderLines_HeaderDiscount_NonNegative];
""");

        foreach (var column in new[] { "ReviewedAtUtc", "ReviewedBy", "ApprovedAtUtc", "ApprovedBy", "SettledAtUtc", "SettledBy" })
            migrationBuilder.DropColumn(column, "tbl_CommissionStatements", "dbo");
        migrationBuilder.DropColumn("BasisKindSnapshot", "tbl_CommissionEntries", "dbo");
        foreach (var column in new[] { "ProductVariantId", "ProductCategoryId", "SaleType", "Priority", "BasisKind" })
            migrationBuilder.DropColumn(column, "tbl_CommissionRules", "dbo");

        migrationBuilder.DropColumn("HeaderDiscountAllocated", "tbl_CustomerOrderLines", "dbo");
        foreach (var column in new[] { "HeaderDiscountType", "HeaderDiscountValue", "HeaderDiscountAmount", "HeaderDiscountReason", "HeaderDiscountApprovedAtUtc", "HeaderDiscountApprovedBy" })
            migrationBuilder.DropColumn(column, "tbl_CustomerOrders", "dbo");
        migrationBuilder.DropColumn("HeaderDiscountAllocated", "tbl_SalesInvoiceLines", "dbo");
        migrationBuilder.DropColumn("BaseHeaderDiscountAllocated", "tbl_SalesInvoiceLines", "dbo");
        foreach (var column in new[] { "HeaderDiscountType", "HeaderDiscountValue", "HeaderDiscountAmount", "BaseHeaderDiscountAmount", "HeaderDiscountReason", "HeaderDiscountApprovedAtUtc", "HeaderDiscountApprovedBy" })
            migrationBuilder.DropColumn(column, "tbl_SalesInvoices", "dbo");

        migrationBuilder.CreateIndex("IX_CommissionRules_Resolution", "tbl_CommissionRules",
            new[] { "EmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive" }, schema: "dbo");
    }
}
