using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261002190100_HROperationsLoansAndAdjustments")]
public sealed class HROperationsLoansAndAdjustments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
SET XACT_ABORT ON;
IF NOT EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'EmployeeLoanCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) EXEC(N'CREATE SEQUENCE [hr].[EmployeeLoanCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF NOT EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'EmployeeAdjustmentCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) EXEC(N'CREATE SEQUENCE [hr].[EmployeeAdjustmentCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF OBJECT_ID(N'[hr].[EmployeeLoans]',N'U') IS NULL
BEGIN
 CREATE TABLE [hr].[EmployeeLoans]([Id] uniqueidentifier NOT NULL,[LoanCode] nvarchar(40) NOT NULL,[EmployeeId] uniqueidentifier NOT NULL,[ContractId] uniqueidentifier NULL,[SalaryStructureId] uniqueidentifier NULL,[LoanDate] date NOT NULL,[CurrencyId] uniqueidentifier NOT NULL,[CurrencyCodeSnapshot] nvarchar(8) NOT NULL,[CurrencySymbolSnapshot] nvarchar(8) NULL,[CurrencyDecimalPlacesSnapshot] tinyint NOT NULL,[PrincipalAmount] decimal(19,4) NOT NULL,[InstallmentCount] int NOT NULL,[FirstInstallmentDate] date NOT NULL,[RepaymentMode] tinyint NOT NULL,[Status] tinyint NOT NULL,[Reason] nvarchar(500) NULL,[PaymentVoucherId] uniqueidentifier NULL,[SubmittedBy] nvarchar(64) NULL,[SubmittedAtUtc] datetimeoffset NULL,[ApprovedBy] nvarchar(64) NULL,[ApprovedAtUtc] datetimeoffset NULL,[DisbursedBy] nvarchar(64) NULL,[DisbursedAtUtc] datetimeoffset NULL,[RejectedBy] nvarchar(64) NULL,[RejectedAtUtc] datetimeoffset NULL,[RejectionReason] nvarchar(500) NULL,[CancelledBy] nvarchar(64) NULL,[CancelledAtUtc] datetimeoffset NULL,[CancellationReason] nvarchar(500) NULL,[CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_EmployeeLoans] PRIMARY KEY([Id]),CONSTRAINT [CK_EmployeeLoans_Principal] CHECK([PrincipalAmount]>0),CONSTRAINT [CK_EmployeeLoans_Count] CHECK([InstallmentCount]>0),CONSTRAINT [FK_EmployeeLoans_Employees] FOREIGN KEY([EmployeeId]) REFERENCES [hr].[Employees]([Id]),CONSTRAINT [FK_EmployeeLoans_Contracts] FOREIGN KEY([ContractId]) REFERENCES [hr].[EmployeeContracts]([Id]),CONSTRAINT [FK_EmployeeLoans_SalaryStructures] FOREIGN KEY([SalaryStructureId]) REFERENCES [hr].[EmployeeSalaryStructures]([Id]),CONSTRAINT [FK_EmployeeLoans_Currencies] FOREIGN KEY([CurrencyId]) REFERENCES [dbo].[tbl_Currencies]([Id]),CONSTRAINT [FK_EmployeeLoans_PaymentVouchers] FOREIGN KEY([PaymentVoucherId]) REFERENCES [dbo].[tbl_PaymentVouchers]([Id]));
 CREATE UNIQUE INDEX [UX_EmployeeLoans_LoanCode] ON [hr].[EmployeeLoans]([LoanCode]);CREATE INDEX [IX_EmployeeLoans_Employee_Status] ON [hr].[EmployeeLoans]([EmployeeId],[Status]);CREATE UNIQUE INDEX [UX_EmployeeLoans_PaymentVoucher] ON [hr].[EmployeeLoans]([PaymentVoucherId]) WHERE [PaymentVoucherId] IS NOT NULL;CREATE INDEX [IX_EmployeeLoans_ContractId] ON [hr].[EmployeeLoans]([ContractId]);CREATE INDEX [IX_EmployeeLoans_SalaryStructureId] ON [hr].[EmployeeLoans]([SalaryStructureId]);CREATE INDEX [IX_EmployeeLoans_CurrencyId] ON [hr].[EmployeeLoans]([CurrencyId]);
END;
IF OBJECT_ID(N'[hr].[EmployeeLoanInstallments]',N'U') IS NULL
BEGIN
 CREATE TABLE [hr].[EmployeeLoanInstallments]([Id] uniqueidentifier NOT NULL,[EmployeeLoanId] uniqueidentifier NOT NULL,[InstallmentSequence] int NOT NULL,[DueDate] date NOT NULL,[Amount] decimal(19,4) NOT NULL,[Status] tinyint NOT NULL,[EmployeePayrollId] uniqueidentifier NULL,[ReceiptVoucherId] uniqueidentifier NULL,[DeductedAtUtc] datetimeoffset NULL,[ExternallyPaidAtUtc] datetimeoffset NULL,[CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_EmployeeLoanInstallments] PRIMARY KEY([Id]),CONSTRAINT [CK_LoanInstallments_Amount] CHECK([Amount]>0),CONSTRAINT [FK_LoanInstallments_Loans] FOREIGN KEY([EmployeeLoanId]) REFERENCES [hr].[EmployeeLoans]([Id]),CONSTRAINT [FK_LoanInstallments_ReceiptVouchers] FOREIGN KEY([ReceiptVoucherId]) REFERENCES [dbo].[tbl_ReceiptVouchers]([Id]));
 CREATE UNIQUE INDEX [UX_LoanInstallments_Loan_Sequence] ON [hr].[EmployeeLoanInstallments]([EmployeeLoanId],[InstallmentSequence]);CREATE INDEX [IX_LoanInstallments_DueDate_Status] ON [hr].[EmployeeLoanInstallments]([DueDate],[Status]);CREATE INDEX [IX_LoanInstallments_ReceiptVoucherId] ON [hr].[EmployeeLoanInstallments]([ReceiptVoucherId]);
END;
IF OBJECT_ID(N'[hr].[EmployeeAdjustments]',N'U') IS NULL
BEGIN
 CREATE TABLE [hr].[EmployeeAdjustments]([Id] uniqueidentifier NOT NULL,[AdjustmentCode] nvarchar(40) NOT NULL,[EmployeeId] uniqueidentifier NOT NULL,[SalaryComponentId] uniqueidentifier NOT NULL,[ComponentCodeSnapshot] nvarchar(32) NOT NULL,[ComponentNameSnapshot] nvarchar(150) NOT NULL,[ComponentTypeSnapshot] tinyint NOT NULL,[DebitPostingRoleSnapshot] nvarchar(50) NULL,[CreditPostingRoleSnapshot] nvarchar(50) NULL,[AdjustmentType] tinyint NOT NULL,[EffectiveDate] date NOT NULL,[CurrencyId] uniqueidentifier NOT NULL,[CurrencyCodeSnapshot] nvarchar(8) NOT NULL,[CurrencySymbolSnapshot] nvarchar(8) NULL,[CurrencyDecimalPlacesSnapshot] tinyint NOT NULL,[Amount] decimal(19,4) NOT NULL,[Status] tinyint NOT NULL,[Reason] nvarchar(500) NOT NULL,[SubmittedBy] nvarchar(64) NULL,[SubmittedAtUtc] datetimeoffset NULL,[ApprovedBy] nvarchar(64) NULL,[ApprovedAtUtc] datetimeoffset NULL,[RejectedBy] nvarchar(64) NULL,[RejectedAtUtc] datetimeoffset NULL,[RejectionReason] nvarchar(500) NULL,[CancelledBy] nvarchar(64) NULL,[CancelledAtUtc] datetimeoffset NULL,[CancellationReason] nvarchar(500) NULL,[EmployeePayrollId] uniqueidentifier NULL,[CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_EmployeeAdjustments] PRIMARY KEY([Id]),CONSTRAINT [CK_EmployeeAdjustments_Amount] CHECK([Amount]>0),CONSTRAINT [FK_EmployeeAdjustments_Employees] FOREIGN KEY([EmployeeId]) REFERENCES [hr].[Employees]([Id]),CONSTRAINT [FK_EmployeeAdjustments_Components] FOREIGN KEY([SalaryComponentId]) REFERENCES [hr].[SalaryComponents]([Id]),CONSTRAINT [FK_EmployeeAdjustments_Currencies] FOREIGN KEY([CurrencyId]) REFERENCES [dbo].[tbl_Currencies]([Id]));
 CREATE UNIQUE INDEX [UX_EmployeeAdjustments_Code] ON [hr].[EmployeeAdjustments]([AdjustmentCode]);CREATE INDEX [IX_Adjustments_Employee_EffectiveDate] ON [hr].[EmployeeAdjustments]([EmployeeId],[EffectiveDate]);CREATE INDEX [IX_Adjustments_Status] ON [hr].[EmployeeAdjustments]([Status]);CREATE INDEX [IX_Adjustments_SalaryComponentId] ON [hr].[EmployeeAdjustments]([SalaryComponentId]);CREATE INDEX [IX_Adjustments_CurrencyId] ON [hr].[EmployeeAdjustments]([CurrencyId]);
END;
""");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
SET XACT_ABORT ON;
IF OBJECT_ID(N'[hr].[EmployeeAdjustments]',N'U') IS NOT NULL DROP TABLE [hr].[EmployeeAdjustments];
IF OBJECT_ID(N'[hr].[EmployeeLoanInstallments]',N'U') IS NOT NULL DROP TABLE [hr].[EmployeeLoanInstallments];
IF OBJECT_ID(N'[hr].[EmployeeLoans]',N'U') IS NOT NULL DROP TABLE [hr].[EmployeeLoans];
IF EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'EmployeeAdjustmentCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) DROP SEQUENCE [hr].[EmployeeAdjustmentCodeSequence];
IF EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'EmployeeLoanCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) DROP SEQUENCE [hr].[EmployeeLoanCodeSequence];
""");
}
