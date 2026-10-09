using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261009143000_CompleteOpticalLaboratoryWorkflow")]
public sealed class CompleteOpticalLaboratoryWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.tbl_OpticalJobs', N'AssignedAtUtc') IS NULL
    ALTER TABLE [dbo].[tbl_OpticalJobs] ADD [AssignedAtUtc] datetimeoffset NULL;
IF COL_LENGTH(N'dbo.tbl_OpticalJobs', N'DeliveredAtUtc') IS NULL
    ALTER TABLE [dbo].[tbl_OpticalJobs] ADD [DeliveredAtUtc] datetimeoffset NULL;
IF COL_LENGTH(N'dbo.tbl_OpticalJobLines', N'GroupId') IS NULL
    ALTER TABLE [dbo].[tbl_OpticalJobLines] ADD [GroupId] uniqueidentifier NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_OpticalJobLines]') AND name = N'IX_OpticalJobLines_OpticalJobId_GroupId')
    CREATE INDEX [IX_OpticalJobLines_OpticalJobId_GroupId] ON [dbo].[tbl_OpticalJobLines]([OpticalJobId],[GroupId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_OpticalJobs]') AND name = N'IX_OpticalJobs_CustomerId')
    CREATE INDEX [IX_OpticalJobs_CustomerId] ON [dbo].[tbl_OpticalJobs]([CustomerId]);

IF COL_LENGTH(N'dbo.tbl_OpticalJobLines', N'RequiresProduction') IS NULL
BEGIN
    ALTER TABLE [dbo].[tbl_OpticalJobLines] ADD [RequiresProduction] bit NOT NULL CONSTRAINT [DF_tbl_OpticalJobLines_RequiresProduction] DEFAULT(1);
    ALTER TABLE [dbo].[tbl_OpticalJobLines] DROP CONSTRAINT [DF_tbl_OpticalJobLines_RequiresProduction];
END;

IF OBJECT_ID(N'[dbo].[tbl_OpticalJobStatusHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_OpticalJobStatusHistory](
        [Id] uniqueidentifier NOT NULL,
        [OpticalJobId] uniqueidentifier NOT NULL,
        [FromStatus] tinyint NULL,
        [ToStatus] tinyint NOT NULL,
        [Reason] nvarchar(1000) NULL,
        [ChangedBy] uniqueidentifier NOT NULL,
        [ChangedAtUtc] datetimeoffset NOT NULL,
        [CorrelationId] nvarchar(100) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        CONSTRAINT [PK_tbl_OpticalJobStatusHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OpticalJobStatusHistory_OpticalJobs] FOREIGN KEY ([OpticalJobId]) REFERENCES [dbo].[tbl_OpticalJobs]([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_OpticalJobStatusHistory_OpticalJobId_ChangedAtUtc] ON [dbo].[tbl_OpticalJobStatusHistory]([OpticalJobId],[ChangedAtUtc]);
END;

IF OBJECT_ID(N'[dbo].[tbl_OpticalQualityChecks]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_OpticalQualityChecks](
        [Id] uniqueidentifier NOT NULL,
        [OpticalJobId] uniqueidentifier NOT NULL,
        [AttemptNumber] int NOT NULL,
        [Result] tinyint NOT NULL,
        [FailureAction] tinyint NULL,
        [GeneralNotes] nvarchar(2000) NULL,
        [CheckedBy] uniqueidentifier NULL,
        [CheckedAtUtc] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_OpticalQualityChecks] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OpticalQualityChecks_AttemptNumber] CHECK ([AttemptNumber] > 0),
        CONSTRAINT [FK_OpticalQualityChecks_OpticalJobs] FOREIGN KEY ([OpticalJobId]) REFERENCES [dbo].[tbl_OpticalJobs]([Id]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [UQ_tbl_OpticalQualityChecks_OpticalJobId_AttemptNumber] ON [dbo].[tbl_OpticalQualityChecks]([OpticalJobId],[AttemptNumber]);
END;

IF OBJECT_ID(N'[dbo].[tbl_OpticalQualityCheckItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_OpticalQualityCheckItems](
        [Id] uniqueidentifier NOT NULL,
        [QualityCheckId] uniqueidentifier NOT NULL,
        [CheckCode] nvarchar(50) NOT NULL,
        [CheckName] nvarchar(200) NOT NULL,
        [Result] tinyint NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [Sequence] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_OpticalQualityCheckItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OpticalQualityCheckItems_Sequence] CHECK ([Sequence] > 0),
        CONSTRAINT [FK_OpticalQualityCheckItems_QualityChecks] FOREIGN KEY ([QualityCheckId]) REFERENCES [dbo].[tbl_OpticalQualityChecks]([Id]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [UQ_tbl_OpticalQualityCheckItems_QualityCheckId_CheckCode] ON [dbo].[tbl_OpticalQualityCheckItems]([QualityCheckId],[CheckCode]);
END;

IF OBJECT_ID(N'[dbo].[tbl_OpticalJobBreakages]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_OpticalJobBreakages](
        [Id] uniqueidentifier NOT NULL,
        [OpticalJobId] uniqueidentifier NOT NULL,
        [OpticalJobLineId] uniqueidentifier NOT NULL,
        [ProductVariantId] uniqueidentifier NOT NULL,
        [Eye] tinyint NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [ReasonCode] nvarchar(50) NOT NULL,
        [ReasonText] nvarchar(1000) NULL,
        [TechnicianId] uniqueidentifier NULL,
        [Status] tinyint NOT NULL,
        [RequiresReplacement] bit NOT NULL,
        [InventoryTransactionId] uniqueidentifier NULL,
        [JournalEntryId] uniqueidentifier NULL,
        [RecordedBy] uniqueidentifier NOT NULL,
        [RecordedAtUtc] datetimeoffset NOT NULL,
        [IdempotencyKey] nvarchar(100) NULL,
        [ClosedAtUtc] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_OpticalJobBreakages] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_tbl_OpticalJobBreakages_Quantity_GT_0] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_tbl_OpticalJobBreakages_OpticalJobs] FOREIGN KEY ([OpticalJobId]) REFERENCES [dbo].[tbl_OpticalJobs]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tbl_OpticalJobBreakages_OpticalJobLines] FOREIGN KEY ([OpticalJobLineId]) REFERENCES [dbo].[tbl_OpticalJobLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tbl_OpticalJobBreakages_ProductVariants] FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tbl_OpticalJobBreakages_Employees] FOREIGN KEY ([TechnicianId]) REFERENCES [hr].[Employees]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tbl_OpticalJobBreakages_InventoryTransactions] FOREIGN KEY ([InventoryTransactionId]) REFERENCES [dbo].[tbl_InventoryTransactions]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tbl_OpticalJobBreakages_JournalEntries] FOREIGN KEY ([JournalEntryId]) REFERENCES [dbo].[tbl_JournalEntries]([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_tbl_OpticalJobBreakages_OpticalJobId] ON [dbo].[tbl_OpticalJobBreakages]([OpticalJobId]);
    CREATE INDEX [IX_tbl_OpticalJobBreakages_Status] ON [dbo].[tbl_OpticalJobBreakages]([Status]);
    CREATE UNIQUE INDEX [UX_tbl_OpticalJobBreakages_Job_IdempotencyKey] ON [dbo].[tbl_OpticalJobBreakages]([OpticalJobId],[IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL;
END;

IF OBJECT_ID(N'[dbo].[tbl_OpticalJobRemakes]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_OpticalJobRemakes](
        [Id] uniqueidentifier NOT NULL,
        [OpticalJobId] uniqueidentifier NOT NULL,
        [SourceQualityCheckId] uniqueidentifier NULL,
        [SourceBreakageId] uniqueidentifier NULL,
        [OpticalJobLineId] uniqueidentifier NOT NULL,
        [ProductVariantId] uniqueidentifier NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [Status] tinyint NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [ReplacementPurchaseRequestLineId] uniqueidentifier NULL,
        [StartedAtUtc] datetimeoffset NULL,
        [CompletedAtUtc] datetimeoffset NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_OpticalJobRemakes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_tbl_OpticalJobRemakes_Quantity_GT_0] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_tbl_OpticalJobRemakes_OpticalJobs] FOREIGN KEY ([OpticalJobId]) REFERENCES [dbo].[tbl_OpticalJobs]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tbl_OpticalJobRemakes_QualityChecks] FOREIGN KEY ([SourceQualityCheckId]) REFERENCES [dbo].[tbl_OpticalQualityChecks]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tbl_OpticalJobRemakes_Breakages] FOREIGN KEY ([SourceBreakageId]) REFERENCES [dbo].[tbl_OpticalJobBreakages]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tbl_OpticalJobRemakes_OpticalJobLines] FOREIGN KEY ([OpticalJobLineId]) REFERENCES [dbo].[tbl_OpticalJobLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tbl_OpticalJobRemakes_ProductVariants] FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tbl_OpticalJobRemakes_PurchaseRequestLines] FOREIGN KEY ([ReplacementPurchaseRequestLineId]) REFERENCES [dbo].[tbl_PurchaseRequestLines]([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_tbl_OpticalJobRemakes_OpticalJobId] ON [dbo].[tbl_OpticalJobRemakes]([OpticalJobId]);
    CREATE INDEX [IX_tbl_OpticalJobRemakes_Status] ON [dbo].[tbl_OpticalJobRemakes]([Status]);
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_OpticalJobRemakes]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_OpticalJobRemakes];
IF OBJECT_ID(N'[dbo].[tbl_OpticalJobBreakages]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_OpticalJobBreakages];
IF OBJECT_ID(N'[dbo].[tbl_OpticalQualityCheckItems]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_OpticalQualityCheckItems];
IF OBJECT_ID(N'[dbo].[tbl_OpticalQualityChecks]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_OpticalQualityChecks];
IF OBJECT_ID(N'[dbo].[tbl_OpticalJobStatusHistory]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_OpticalJobStatusHistory];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_OpticalJobLines]') AND name = N'IX_OpticalJobLines_OpticalJobId_GroupId') DROP INDEX [IX_OpticalJobLines_OpticalJobId_GroupId] ON [dbo].[tbl_OpticalJobLines];
IF COL_LENGTH(N'dbo.tbl_OpticalJobLines', N'GroupId') IS NOT NULL ALTER TABLE [dbo].[tbl_OpticalJobLines] DROP COLUMN [GroupId];
IF COL_LENGTH(N'dbo.tbl_OpticalJobLines', N'RequiresProduction') IS NOT NULL ALTER TABLE [dbo].[tbl_OpticalJobLines] DROP COLUMN [RequiresProduction];
IF COL_LENGTH(N'dbo.tbl_OpticalJobs', N'DeliveredAtUtc') IS NOT NULL ALTER TABLE [dbo].[tbl_OpticalJobs] DROP COLUMN [DeliveredAtUtc];
IF COL_LENGTH(N'dbo.tbl_OpticalJobs', N'AssignedAtUtc') IS NOT NULL ALTER TABLE [dbo].[tbl_OpticalJobs] DROP COLUMN [AssignedAtUtc];
""");
    }
}
