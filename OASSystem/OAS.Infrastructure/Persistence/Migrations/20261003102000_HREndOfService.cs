using Microsoft.EntityFrameworkCore.Infrastructure;using Microsoft.EntityFrameworkCore.Migrations;using OAS.Infrastructure.Persistence;
#nullable disable
namespace OAS.Infrastructure.Persistence.Migrations;
[DbContext(typeof(OasDbContext))][Migration("20261003102000_HREndOfService")]
public sealed class HREndOfService:Migration
{
 protected override void Up(MigrationBuilder m)
 {
  m.Sql("""
SET XACT_ABORT ON;
IF NOT EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'EndOfServiceCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) EXEC(N'CREATE SEQUENCE [hr].[EndOfServiceCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF COL_LENGTH('hr.LeaveTypes','IsEncashableOnTermination') IS NULL ALTER TABLE [hr].[LeaveTypes] ADD [IsEncashableOnTermination] bit NOT NULL CONSTRAINT [DF_LeaveTypes_IsEncashableOnTermination] DEFAULT(0);
IF COL_LENGTH('hr.EmployeeLeaveBalances','SettledDays') IS NULL ALTER TABLE [hr].[EmployeeLeaveBalances] ADD [SettledDays] decimal(8,2) NOT NULL CONSTRAINT [DF_LeaveBalances_SettledDays] DEFAULT(0);
""");

  m.Sql("""
SET XACT_ABORT ON;
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_LeaveBalances_NonNegative' AND parent_object_id=OBJECT_ID(N'[hr].[EmployeeLeaveBalances]')) ALTER TABLE [hr].[EmployeeLeaveBalances] DROP CONSTRAINT [CK_LeaveBalances_NonNegative];
ALTER TABLE [hr].[EmployeeLeaveBalances] ADD CONSTRAINT [CK_LeaveBalances_NonNegative] CHECK([OpeningBalanceDays]>=0 AND [AccruedDays]>=0 AND [UsedDays]>=0 AND [SettledDays]>=0);
IF OBJECT_ID(N'[hr].[EndOfServiceSettlements]',N'U') IS NULL BEGIN
 CREATE TABLE [hr].[EndOfServiceSettlements]([Id] uniqueidentifier NOT NULL,[SettlementCode] nvarchar(40) NOT NULL,[EmployeeId] uniqueidentifier NOT NULL,[ContractId] uniqueidentifier NULL,[FinalEmployeePayrollId] uniqueidentifier NULL,[LastWorkingDate] date NOT NULL,[Status] tinyint NOT NULL,[CurrencyId] uniqueidentifier NOT NULL,[CurrencyCodeSnapshot] nvarchar(8) NOT NULL,[CurrencySymbolSnapshot] nvarchar(12) NULL,[CurrencyDecimalPlacesSnapshot] tinyint NOT NULL,[OutstandingPayrollAmountSnapshot] decimal(19,4) NOT NULL,[LeaveSettlementAmount] decimal(19,4) NOT NULL,[EndOfServiceBenefitAmount] decimal(19,4) NOT NULL,[OtherEarningsAmount] decimal(19,4) NOT NULL,[LoanDeductionAmount] decimal(19,4) NOT NULL,[OtherDeductionsAmount] decimal(19,4) NOT NULL,[GrossSettlementAmount] decimal(19,4) NOT NULL,[NetSettlementAmount] decimal(19,4) NOT NULL,[BaseGrossSettlementAmount] decimal(19,4) NOT NULL,[BaseNetSettlementAmount] decimal(19,4) NOT NULL,[PostingExchangeRate] decimal(19,8) NULL,[PostingExchangeRateDate] date NULL,[PostingExchangeRateType] tinyint NULL,[PostingExchangeRateSource] tinyint NULL,[JournalEntryId] uniqueidentifier NULL,[Reason] nvarchar(500) NULL,[CalculatedBy] nvarchar(64) NULL,[CalculatedAtUtc] datetimeoffset NULL,[ReviewedBy] nvarchar(64) NULL,[ReviewedAtUtc] datetimeoffset NULL,[ApprovedBy] nvarchar(64) NULL,[ApprovedAtUtc] datetimeoffset NULL,[PostedBy] nvarchar(64) NULL,[PostedAtUtc] datetimeoffset NULL,[CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_EndOfServiceSettlements] PRIMARY KEY([Id]),CONSTRAINT [CK_EndOfService_Amounts] CHECK([OutstandingPayrollAmountSnapshot]>=0 AND [LeaveSettlementAmount]>=0 AND [EndOfServiceBenefitAmount]>=0 AND [OtherEarningsAmount]>=0 AND [LoanDeductionAmount]>=0 AND [OtherDeductionsAmount]>=0 AND [GrossSettlementAmount]>=0 AND [NetSettlementAmount]>=0 AND [BaseGrossSettlementAmount]>=0 AND [BaseNetSettlementAmount]>=0),CONSTRAINT [CK_EndOfService_Rate] CHECK([PostingExchangeRate] IS NULL OR [PostingExchangeRate]>0),CONSTRAINT [FK_EndOfService_Employee] FOREIGN KEY([EmployeeId]) REFERENCES [hr].[Employees]([Id]),CONSTRAINT [FK_EndOfService_Contract] FOREIGN KEY([ContractId]) REFERENCES [hr].[EmployeeContracts]([Id]),CONSTRAINT [FK_EndOfService_FinalPayroll] FOREIGN KEY([FinalEmployeePayrollId]) REFERENCES [hr].[EmployeePayrolls]([Id]),CONSTRAINT [FK_EndOfService_Currency] FOREIGN KEY([CurrencyId]) REFERENCES [dbo].[tbl_Currencies]([Id]),CONSTRAINT [FK_EndOfService_Journal] FOREIGN KEY([JournalEntryId]) REFERENCES [dbo].[tbl_JournalEntries]([Id]));CREATE UNIQUE INDEX [UX_EndOfService_SettlementCode] ON [hr].[EndOfServiceSettlements]([SettlementCode]);CREATE UNIQUE INDEX [UX_EndOfService_Employee_Active] ON [hr].[EndOfServiceSettlements]([EmployeeId]) WHERE [Status] <> 7;
END;
IF OBJECT_ID(N'[hr].[EndOfServiceSettlementLines]',N'U') IS NULL BEGIN
 CREATE TABLE [hr].[EndOfServiceSettlementLines]([Id] uniqueidentifier NOT NULL,[EndOfServiceSettlementId] uniqueidentifier NOT NULL,[LineSequence] int NOT NULL,[LineType] tinyint NOT NULL,[SourceDocumentType] nvarchar(80) NULL,[SourceDocumentId] uniqueidentifier NULL,[Description] nvarchar(500) NOT NULL,[Quantity] decimal(18,4) NULL,[Rate] decimal(19,6) NULL,[Amount] decimal(19,4) NOT NULL,[DebitPostingRole] nvarchar(50) NULL,[CreditPostingRole] nvarchar(50) NULL,[CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_EndOfServiceSettlementLines] PRIMARY KEY([Id]),CONSTRAINT [CK_EndOfServiceLines_Amount] CHECK([Amount]>=0),CONSTRAINT [FK_EndOfServiceLines_Settlement] FOREIGN KEY([EndOfServiceSettlementId]) REFERENCES [hr].[EndOfServiceSettlements]([Id]));CREATE UNIQUE INDEX [UX_EndOfServiceLines_Settlement_Sequence] ON [hr].[EndOfServiceSettlementLines]([EndOfServiceSettlementId],[LineSequence]);
END;
IF COL_LENGTH('hr.EmployeeLoanInstallments','EndOfServiceSettlementId') IS NULL ALTER TABLE [hr].[EmployeeLoanInstallments] ADD [EndOfServiceSettlementId] uniqueidentifier NULL;
""");

  m.Sql("""
SET XACT_ABORT ON;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_LoanInstallments_EndOfServiceSettlement' AND parent_object_id=OBJECT_ID(N'[hr].[EmployeeLoanInstallments]')) ALTER TABLE [hr].[EmployeeLoanInstallments] ADD CONSTRAINT [FK_LoanInstallments_EndOfServiceSettlement] FOREIGN KEY([EndOfServiceSettlementId]) REFERENCES [hr].[EndOfServiceSettlements]([Id]);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_LoanInstallments_EndOfServiceSettlementId' AND object_id=OBJECT_ID(N'[hr].[EmployeeLoanInstallments]')) CREATE INDEX [IX_LoanInstallments_EndOfServiceSettlementId] ON [hr].[EmployeeLoanInstallments]([EndOfServiceSettlementId]);
""");
 }

 protected override void Down(MigrationBuilder m)=>m.Sql("""
SET XACT_ABORT ON;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_LoanInstallments_EndOfServiceSettlement') ALTER TABLE [hr].[EmployeeLoanInstallments] DROP CONSTRAINT [FK_LoanInstallments_EndOfServiceSettlement];
IF EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_LoanInstallments_EndOfServiceSettlementId' AND object_id=OBJECT_ID(N'[hr].[EmployeeLoanInstallments]')) DROP INDEX [IX_LoanInstallments_EndOfServiceSettlementId] ON [hr].[EmployeeLoanInstallments];
IF COL_LENGTH('hr.EmployeeLoanInstallments','EndOfServiceSettlementId') IS NOT NULL ALTER TABLE [hr].[EmployeeLoanInstallments] DROP COLUMN [EndOfServiceSettlementId];
IF OBJECT_ID(N'[hr].[EndOfServiceSettlementLines]',N'U') IS NOT NULL DROP TABLE [hr].[EndOfServiceSettlementLines];
IF OBJECT_ID(N'[hr].[EndOfServiceSettlements]',N'U') IS NOT NULL DROP TABLE [hr].[EndOfServiceSettlements];
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_LeaveBalances_NonNegative' AND parent_object_id=OBJECT_ID(N'[hr].[EmployeeLeaveBalances]')) ALTER TABLE [hr].[EmployeeLeaveBalances] DROP CONSTRAINT [CK_LeaveBalances_NonNegative];
IF EXISTS(SELECT 1 FROM sys.default_constraints WHERE name=N'DF_LeaveBalances_SettledDays') ALTER TABLE [hr].[EmployeeLeaveBalances] DROP CONSTRAINT [DF_LeaveBalances_SettledDays];
IF COL_LENGTH('hr.EmployeeLeaveBalances','SettledDays') IS NOT NULL ALTER TABLE [hr].[EmployeeLeaveBalances] DROP COLUMN [SettledDays];
ALTER TABLE [hr].[EmployeeLeaveBalances] ADD CONSTRAINT [CK_LeaveBalances_NonNegative] CHECK([OpeningBalanceDays]>=0 AND [AccruedDays]>=0 AND [UsedDays]>=0);
IF EXISTS(SELECT 1 FROM sys.default_constraints WHERE name=N'DF_LeaveTypes_IsEncashableOnTermination') ALTER TABLE [hr].[LeaveTypes] DROP CONSTRAINT [DF_LeaveTypes_IsEncashableOnTermination];
IF COL_LENGTH('hr.LeaveTypes','IsEncashableOnTermination') IS NOT NULL ALTER TABLE [hr].[LeaveTypes] DROP COLUMN [IsEncashableOnTermination];
IF EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'EndOfServiceCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) DROP SEQUENCE [hr].[EndOfServiceCodeSequence];
""");
}
