using Microsoft.EntityFrameworkCore.Infrastructure;using Microsoft.EntityFrameworkCore.Migrations;using OAS.Infrastructure.Persistence;
#nullable disable
namespace OAS.Infrastructure.Persistence.Migrations;
[DbContext(typeof(OasDbContext))][Migration("20261003101000_HRPayrollAccountingAndPayments")]
public sealed class HRPayrollAccountingAndPayments:Migration
{
 protected override void Up(MigrationBuilder m)
 {
  m.Sql("""
SET XACT_ABORT ON;
IF COL_LENGTH('dbo.tbl_PaymentAllocations','TargetBaseAllocatedAmount') IS NULL ALTER TABLE [dbo].[tbl_PaymentAllocations] ADD [TargetBaseAllocatedAmount] decimal(19,4) NULL;
IF COL_LENGTH('dbo.tbl_PaymentVoucherLines','CounterpartyBaseAmount') IS NULL ALTER TABLE [dbo].[tbl_PaymentVoucherLines] ADD [CounterpartyBaseAmount] decimal(19,4) NULL;
IF COL_LENGTH('dbo.tbl_PaymentVoucherLines','RealizedExchangeDifferenceBase') IS NULL ALTER TABLE [dbo].[tbl_PaymentVoucherLines] ADD [RealizedExchangeDifferenceBase] decimal(19,4) NULL;
""");

  m.Sql("""
SET XACT_ABORT ON;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_PaymentAllocations_TargetBaseAmount_Positive' AND parent_object_id=OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]')) ALTER TABLE [dbo].[tbl_PaymentAllocations] ADD CONSTRAINT [CK_PaymentAllocations_TargetBaseAmount_Positive] CHECK([TargetBaseAllocatedAmount] IS NULL OR [TargetBaseAllocatedAmount]>0);
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_PaymentVoucherLines_CounterpartyBaseAmount_Positive' AND parent_object_id=OBJECT_ID(N'[dbo].[tbl_PaymentVoucherLines]')) ALTER TABLE [dbo].[tbl_PaymentVoucherLines] ADD CONSTRAINT [CK_PaymentVoucherLines_CounterpartyBaseAmount_Positive] CHECK([CounterpartyBaseAmount] IS NULL OR [CounterpartyBaseAmount]>0);
""");
 }
 protected override void Down(MigrationBuilder m)=>m.Sql("""
SET XACT_ABORT ON;
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_PaymentVoucherLines_CounterpartyBaseAmount_Positive') ALTER TABLE [dbo].[tbl_PaymentVoucherLines] DROP CONSTRAINT [CK_PaymentVoucherLines_CounterpartyBaseAmount_Positive];
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_PaymentAllocations_TargetBaseAmount_Positive') ALTER TABLE [dbo].[tbl_PaymentAllocations] DROP CONSTRAINT [CK_PaymentAllocations_TargetBaseAmount_Positive];
IF COL_LENGTH('dbo.tbl_PaymentVoucherLines','RealizedExchangeDifferenceBase') IS NOT NULL ALTER TABLE [dbo].[tbl_PaymentVoucherLines] DROP COLUMN [RealizedExchangeDifferenceBase];IF COL_LENGTH('dbo.tbl_PaymentVoucherLines','CounterpartyBaseAmount') IS NOT NULL ALTER TABLE [dbo].[tbl_PaymentVoucherLines] DROP COLUMN [CounterpartyBaseAmount];IF COL_LENGTH('dbo.tbl_PaymentAllocations','TargetBaseAllocatedAmount') IS NOT NULL ALTER TABLE [dbo].[tbl_PaymentAllocations] DROP COLUMN [TargetBaseAllocatedAmount];
""");
}
