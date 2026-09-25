using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260923160000_AccountingCustomersSuppliersMaster")]
public sealed class AccountingCustomersSuppliersMaster : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

-- Fail loudly if the legacy mapping contradicts the real account hierarchy.
IF OBJECT_ID(N'[dbo].[tbl_CustomerAccounts]', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1 FROM [dbo].[tbl_CustomerAccounts] ca
    INNER JOIN [dbo].[tbl_Accounts] a ON a.Id=ca.AccountId
    WHERE a.ParentAccountId IS NOT NULL AND a.ParentAccountId<>ca.ControlAccountId)
    THROW 51001, 'Customer account parent conflicts with legacy ControlAccountId.', 1;

IF OBJECT_ID(N'[dbo].[tbl_SupplierAccounts]', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1 FROM [dbo].[tbl_SupplierAccounts] sa
    INNER JOIN [dbo].[tbl_Accounts] a ON a.Id=sa.AccountId
    WHERE a.ParentAccountId IS NOT NULL AND a.ParentAccountId<>sa.ControlAccountId)
    THROW 51002, 'Supplier account parent conflicts with legacy ControlAccountId.', 1;

-- Backfill the authoritative parent only when it was missing in the account itself.
IF OBJECT_ID(N'[dbo].[tbl_CustomerAccounts]', N'U') IS NOT NULL
    UPDATE a SET ParentAccountId=ca.ControlAccountId
    FROM [dbo].[tbl_Accounts] a INNER JOIN [dbo].[tbl_CustomerAccounts] ca ON ca.AccountId=a.Id
    WHERE a.ParentAccountId IS NULL;

IF OBJECT_ID(N'[dbo].[tbl_SupplierAccounts]', N'U') IS NOT NULL
    UPDATE a SET ParentAccountId=sa.ControlAccountId
    FROM [dbo].[tbl_Accounts] a INNER JOIN [dbo].[tbl_SupplierAccounts] sa ON sa.AccountId=a.Id
    WHERE a.ParentAccountId IS NULL;

-- IMPORTANT: legacy CustomerAccount/SupplierAccount records were created by the old
-- module without enforcing AccountClass/AccountType/NormalBalance/IsControlAccount on
-- ControlAccountId. Do not reject those historical mappings during migration.
-- The authoritative relationship is preserved through Account.ParentAccountId.
-- Strict parent-account rules are enforced only for NEW customers/suppliers by
-- PartyAccountProvisioningService after this migration.
--
-- We still fail above (51001/51002) when the existing Account.ParentAccountId
-- explicitly contradicts the legacy ControlAccountId, because that is a real data
-- conflict that must not be silently rewritten.

IF OBJECT_ID(N'[dbo].[tbl_Customers]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_Customers](
        [Id] uniqueidentifier NOT NULL,
        [CustomerCode] nvarchar(32) NOT NULL,
        [AccountId] uniqueidentifier NOT NULL,
        [EntityType] tinyint NOT NULL CONSTRAINT [DF_Customers_EntityType] DEFAULT(0),
        [NameAr] nvarchar(150) NOT NULL,
        [NameEn] nvarchar(150) NULL,
        [TradeName] nvarchar(150) NULL,
        [NationalId] nvarchar(50) NULL,
        [CommercialRegistrationNo] nvarchar(50) NULL,
        [TaxNumber] nvarchar(50) NULL,
        [DateOfBirth] date NULL,
        [Gender] tinyint NOT NULL CONSTRAINT [DF_Customers_Gender] DEFAULT(0),
        [ContactPersonName] nvarchar(150) NULL,[ContactPersonTitle] nvarchar(100) NULL,
        [Phone] nvarchar(32) NULL,[Mobile] nvarchar(32) NULL,[AlternatePhone] nvarchar(32) NULL,[WhatsAppNumber] nvarchar(32) NULL,
        [Email] nvarchar(256) NULL,[Website] nvarchar(300) NULL,[PreferredContactMethod] tinyint NOT NULL CONSTRAINT [DF_Customers_Contact] DEFAULT(0),
        [Country] nvarchar(100) NULL,[Governorate] nvarchar(100) NULL,[City] nvarchar(100) NULL,[District] nvarchar(100) NULL,
        [Street] nvarchar(150) NULL,[Building] nvarchar(100) NULL,[PostalCode] nvarchar(24) NULL,[AddressDetails] nvarchar(300) NULL,
        [IsCreditAllowed] bit NOT NULL CONSTRAINT [DF_Customers_CreditAllowed] DEFAULT(0),[CreditLimit] decimal(19,4) NOT NULL CONSTRAINT [DF_Customers_CreditLimit] DEFAULT(0),
        [PaymentTermDays] int NOT NULL CONSTRAINT [DF_Customers_PaymentDays] DEFAULT(0),[CustomerSince] date NULL,[IsActive] bit NOT NULL,
        [Notes] nvarchar(1000) NULL,[CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY([Id]),
        CONSTRAINT [FK_Customers_Account] FOREIGN KEY([AccountId]) REFERENCES [dbo].[tbl_Accounts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_Customers_CreditLimit] CHECK([CreditLimit]>=0),CONSTRAINT [CK_Customers_PaymentTermDays] CHECK([PaymentTermDays]>=0)
    );
END;

IF OBJECT_ID(N'[dbo].[tbl_Suppliers]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_Suppliers](
        [Id] uniqueidentifier NOT NULL,[SupplierCode] nvarchar(32) NOT NULL,[AccountId] uniqueidentifier NOT NULL,
        [EntityType] tinyint NOT NULL CONSTRAINT [DF_Suppliers_EntityType] DEFAULT(0),[SupplierScope] tinyint NOT NULL CONSTRAINT [DF_Suppliers_Scope] DEFAULT(0),
        [NameAr] nvarchar(150) NOT NULL,[NameEn] nvarchar(150) NULL,[TradeName] nvarchar(150) NULL,[NationalId] nvarchar(50) NULL,
        [CommercialRegistrationNo] nvarchar(50) NULL,[TaxNumber] nvarchar(50) NULL,[ContactPersonName] nvarchar(150) NULL,[ContactPersonTitle] nvarchar(100) NULL,
        [Phone] nvarchar(32) NULL,[Mobile] nvarchar(32) NULL,[AlternatePhone] nvarchar(32) NULL,[WhatsAppNumber] nvarchar(32) NULL,[Email] nvarchar(256) NULL,[Website] nvarchar(300) NULL,
        [PreferredContactMethod] tinyint NOT NULL CONSTRAINT [DF_Suppliers_Contact] DEFAULT(0),[Country] nvarchar(100) NULL,[Governorate] nvarchar(100) NULL,[City] nvarchar(100) NULL,[District] nvarchar(100) NULL,
        [Street] nvarchar(150) NULL,[Building] nvarchar(100) NULL,[PostalCode] nvarchar(24) NULL,[AddressDetails] nvarchar(300) NULL,
        [CreditLimit] decimal(19,4) NOT NULL CONSTRAINT [DF_Suppliers_CreditLimit] DEFAULT(0),[PaymentTermDays] int NOT NULL CONSTRAINT [DF_Suppliers_PaymentDays] DEFAULT(0),
        [DefaultLeadTimeDays] int NULL,[SupplierSince] date NULL,[IsActive] bit NOT NULL,[Notes] nvarchar(1000) NULL,
        [CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Suppliers] PRIMARY KEY([Id]),CONSTRAINT [FK_Suppliers_Account] FOREIGN KEY([AccountId]) REFERENCES [dbo].[tbl_Accounts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_Suppliers_CreditLimit] CHECK([CreditLimit]>=0),CONSTRAINT [CK_Suppliers_PaymentTermDays] CHECK([PaymentTermDays]>=0),CONSTRAINT [CK_Suppliers_LeadTime] CHECK([DefaultLeadTimeDays] IS NULL OR [DefaultLeadTimeDays]>=0)
    );
END;

-- Preserve legacy IDs and account links. Account.Code is deliberately not changed.
IF OBJECT_ID(N'[dbo].[tbl_CustomerAccounts]', N'U') IS NOT NULL
BEGIN
    ;WITH legacy AS (
      SELECT ca.CustomerId,ca.AccountId,ca.IsActive,ca.CreatedAt,ca.CreatedBy,ca.UpdatedAt,ca.UpdatedBy,ca.CreatedFromDevice,ca.UpdatedFromDevice,a.NameAr,a.NameEn,
             ROW_NUMBER() OVER(ORDER BY ca.CustomerId) AS rn
      FROM [dbo].[tbl_CustomerAccounts] ca INNER JOIN [dbo].[tbl_Accounts] a ON a.Id=ca.AccountId)
    INSERT INTO [dbo].[tbl_Customers]([Id],[CustomerCode],[AccountId],[EntityType],[NameAr],[NameEn],[Gender],[PreferredContactMethod],[IsCreditAllowed],[CreditLimit],[PaymentTermDays],[IsActive],[CreatedAt],[CreatedBy],[UpdatedAt],[UpdatedBy],[CreatedFromDevice],[UpdatedFromDevice])
    SELECT CustomerId,N'CUS-'+RIGHT(N'000000'+CONVERT(nvarchar(20),rn),6),AccountId,0,NameAr,NameEn,0,0,0,0,0,IsActive,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy,CreatedFromDevice,UpdatedFromDevice
    FROM legacy l WHERE NOT EXISTS(SELECT 1 FROM [dbo].[tbl_Customers] c WHERE c.Id=l.CustomerId);
END;

IF OBJECT_ID(N'[dbo].[tbl_SupplierAccounts]', N'U') IS NOT NULL
BEGIN
    ;WITH legacy AS (
      SELECT sa.SupplierId,sa.AccountId,sa.IsActive,sa.CreatedAt,sa.CreatedBy,sa.UpdatedAt,sa.UpdatedBy,sa.CreatedFromDevice,sa.UpdatedFromDevice,a.NameAr,a.NameEn,
             ROW_NUMBER() OVER(ORDER BY sa.SupplierId) AS rn
      FROM [dbo].[tbl_SupplierAccounts] sa INNER JOIN [dbo].[tbl_Accounts] a ON a.Id=sa.AccountId)
    INSERT INTO [dbo].[tbl_Suppliers]([Id],[SupplierCode],[AccountId],[EntityType],[SupplierScope],[NameAr],[NameEn],[PreferredContactMethod],[CreditLimit],[PaymentTermDays],[IsActive],[CreatedAt],[CreatedBy],[UpdatedAt],[UpdatedBy],[CreatedFromDevice],[UpdatedFromDevice])
    SELECT SupplierId,N'SUP-'+RIGHT(N'000000'+CONVERT(nvarchar(20),rn),6),AccountId,0,0,NameAr,NameEn,0,0,0,IsActive,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy,CreatedFromDevice,UpdatedFromDevice
    FROM legacy l WHERE NOT EXISTS(SELECT 1 FROM [dbo].[tbl_Suppliers] s WHERE s.Id=l.SupplierId);
END;

-- Historical vouchers/journal lines could reference CustomerId/SupplierId values from
-- external modules without a corresponding CustomerAccount/SupplierAccount mapping.
-- Preserve those references by creating inactive migrated masters and managed subledger
-- accounts instead of nulling/deleting historical references.
DECLARE @MigrationNow datetimeoffset = TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00');
DECLARE @MigrationActor nvarchar(64) = N'migration:party-backfill';

------------------------------------------------------------------------
-- Backfill orphan CUSTOMER references.
------------------------------------------------------------------------
IF EXISTS
(
    SELECT 1
    FROM
    (
        SELECT [CustomerId] FROM [dbo].[tbl_ReceiptVouchers] WHERE [CustomerId] IS NOT NULL
        UNION
        SELECT [CustomerId] FROM [dbo].[tbl_JournalEntryLines] WHERE [CustomerId] IS NOT NULL
    ) r
    WHERE NOT EXISTS (SELECT 1 FROM [dbo].[tbl_Customers] c WHERE c.[Id] = r.[CustomerId])
)
BEGIN
    DECLARE @CustomerParentId uniqueidentifier = NULL;

    -- Prefer a currently valid AR control account.
    SELECT TOP (1) @CustomerParentId = a.[Id]
    FROM [dbo].[tbl_Accounts] a
    WHERE a.[IsActive] = 1
      AND a.[AccountClass] = 1      -- Asset
      AND a.[AccountType] = 3       -- Control
      AND a.[NormalBalance] = 1     -- Debit
      AND a.[IsControlAccount] = 1
    ORDER BY a.[Code];

    -- Otherwise preserve the most commonly used legacy customer control account.
    IF @CustomerParentId IS NULL AND OBJECT_ID(N'[dbo].[tbl_CustomerAccounts]', N'U') IS NOT NULL
    BEGIN
        SELECT TOP (1) @CustomerParentId = ca.[ControlAccountId]
        FROM [dbo].[tbl_CustomerAccounts] ca
        INNER JOIN [dbo].[tbl_Accounts] a ON a.[Id] = ca.[ControlAccountId]
        GROUP BY ca.[ControlAccountId]
        ORDER BY COUNT_BIG(*) DESC, ca.[ControlAccountId];
    END;

    -- Last resort: create a dedicated valid control account for migrated historical customers.
    IF @CustomerParentId IS NULL
    BEGIN
        SET @CustomerParentId = NEWID();
        DECLARE @LegacyCustomerControlCode nvarchar(30) =
            N'AR-LEGACY-' + LEFT(REPLACE(CONVERT(nvarchar(36), @CustomerParentId), N'-', N''), 8);

        INSERT INTO [dbo].[tbl_Accounts]
        (
            [Id],[Code],[NameAr],[NameEn],[ParentAccountId],[Level],[AccountClass],[AccountType],
            [NormalBalance],[IsPostingAccount],[IsControlAccount],[AllowManualPosting],
            [IsSystemAccount],[IsActive],[EffectiveDate],
            [CreatedAt],[CreatedBy],[UpdatedAt],[UpdatedBy],[CreatedFromDevice],[UpdatedFromDevice]
        )
        VALUES
        (
            @CustomerParentId,@LegacyCustomerControlCode,N'حساب العملاء المرحّلين',N'Legacy Migrated Customers',
            NULL,1,1,3,1,0,1,0,1,1,NULL,
            @MigrationNow,@MigrationActor,NULL,NULL,NULL,NULL
        );
    END;

    DECLARE @CustomerCodeBase bigint = ISNULL
    (
        (SELECT MAX(TRY_CONVERT(bigint, SUBSTRING([CustomerCode], 5, 20)))
         FROM [dbo].[tbl_Customers]
         WHERE [CustomerCode] LIKE N'CUS-%'), 0
    );

    DECLARE @ArCodeBase bigint = ISNULL
    (
        (SELECT MAX(TRY_CONVERT(bigint, SUBSTRING([Code], 4, 20)))
         FROM [dbo].[tbl_Accounts]
         WHERE [Code] LIKE N'AR-%'), 0
    );

    DECLARE @OrphanCustomers TABLE
    (
        [CustomerId] uniqueidentifier NOT NULL PRIMARY KEY,
        [AccountId] uniqueidentifier NOT NULL,
        [CustomerCode] nvarchar(32) NOT NULL,
        [AccountCode] nvarchar(30) NOT NULL,
        [NameAr] nvarchar(150) NOT NULL
    );

    ;WITH refs AS
    (
        SELECT v.[CustomerId], NULLIF(LTRIM(RTRIM(v.[ReceivedFrom])), N'') AS [NameAr]
        FROM [dbo].[tbl_ReceiptVouchers] v
        WHERE v.[CustomerId] IS NOT NULL

        UNION ALL

        SELECT l.[CustomerId], NULLIF(LTRIM(RTRIM(l.[Description])), N'') AS [NameAr]
        FROM [dbo].[tbl_JournalEntryLines] l
        WHERE l.[CustomerId] IS NOT NULL
    ), grouped AS
    (
        SELECT r.[CustomerId], MAX(r.[NameAr]) AS [NameAr]
        FROM refs r
        WHERE NOT EXISTS (SELECT 1 FROM [dbo].[tbl_Customers] c WHERE c.[Id] = r.[CustomerId])
        GROUP BY r.[CustomerId]
    ), numbered AS
    (
        SELECT g.[CustomerId], g.[NameAr], ROW_NUMBER() OVER (ORDER BY g.[CustomerId]) AS rn
        FROM grouped g
    )
    INSERT INTO @OrphanCustomers ([CustomerId],[AccountId],[CustomerCode],[AccountCode],[NameAr])
    SELECT
        n.[CustomerId],
        NEWID(),
        N'CUS-' + RIGHT(N'000000' + CONVERT(nvarchar(20), @CustomerCodeBase + n.rn), 6),
        N'AR-'  + RIGHT(N'000000' + CONVERT(nvarchar(20), @ArCodeBase + n.rn), 6),
        LEFT(COALESCE(n.[NameAr], N'عميل مرحّل - ' + CONVERT(nvarchar(36), n.[CustomerId])), 150)
    FROM numbered n;

    INSERT INTO [dbo].[tbl_Accounts]
    (
        [Id],[Code],[NameAr],[NameEn],[ParentAccountId],[Level],[AccountClass],[AccountType],
        [NormalBalance],[IsPostingAccount],[IsControlAccount],[AllowManualPosting],
        [IsSystemAccount],[IsActive],[EffectiveDate],
        [CreatedAt],[CreatedBy],[UpdatedAt],[UpdatedBy],[CreatedFromDevice],[UpdatedFromDevice]
    )
    SELECT
        o.[AccountId],o.[AccountCode],o.[NameAr],NULL,@CustomerParentId,
        CONVERT(tinyint, CASE WHEN p.[Level] < 255 THEN p.[Level] + 1 ELSE 255 END),
        1,4,1,1,0,0,0,0,NULL,
        @MigrationNow,@MigrationActor,NULL,NULL,NULL,NULL
    FROM @OrphanCustomers o
    INNER JOIN [dbo].[tbl_Accounts] p ON p.[Id] = @CustomerParentId;

    INSERT INTO [dbo].[tbl_Customers]
    (
        [Id],[CustomerCode],[AccountId],[EntityType],[NameAr],[NameEn],[TradeName],
        [NationalId],[CommercialRegistrationNo],[TaxNumber],[DateOfBirth],[Gender],
        [ContactPersonName],[ContactPersonTitle],[Phone],[Mobile],[AlternatePhone],[WhatsAppNumber],
        [Email],[Website],[PreferredContactMethod],[Country],[Governorate],[City],[District],[Street],[Building],[PostalCode],[AddressDetails],
        [IsCreditAllowed],[CreditLimit],[PaymentTermDays],[CustomerSince],[IsActive],[Notes],
        [CreatedAt],[CreatedBy],[UpdatedAt],[UpdatedBy],[CreatedFromDevice],[UpdatedFromDevice]
    )
    SELECT
        o.[CustomerId],o.[CustomerCode],o.[AccountId],0,o.[NameAr],NULL,NULL,
        NULL,NULL,NULL,NULL,0,
        NULL,NULL,NULL,NULL,NULL,NULL,
        NULL,NULL,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,
        0,0,0,NULL,0,N'تم إنشاء هذا السجل تلقائيًا للحفاظ على مرجع محاسبي تاريخي أثناء الترحيل.',
        @MigrationNow,@MigrationActor,NULL,NULL,NULL,NULL
    FROM @OrphanCustomers o;
END;

------------------------------------------------------------------------
-- Backfill orphan SUPPLIER references.
------------------------------------------------------------------------
IF EXISTS
(
    SELECT 1
    FROM
    (
        SELECT [SupplierId] FROM [dbo].[tbl_PaymentVouchers] WHERE [SupplierId] IS NOT NULL
        UNION
        SELECT [SupplierId] FROM [dbo].[tbl_JournalEntryLines] WHERE [SupplierId] IS NOT NULL
    ) r
    WHERE NOT EXISTS (SELECT 1 FROM [dbo].[tbl_Suppliers] s WHERE s.[Id] = r.[SupplierId])
)
BEGIN
    DECLARE @SupplierParentId uniqueidentifier = NULL;

    -- Prefer a currently valid AP control account.
    SELECT TOP (1) @SupplierParentId = a.[Id]
    FROM [dbo].[tbl_Accounts] a
    WHERE a.[IsActive] = 1
      AND a.[AccountClass] = 2      -- Liability
      AND a.[AccountType] = 3       -- Control
      AND a.[NormalBalance] = 2     -- Credit
      AND a.[IsControlAccount] = 1
    ORDER BY a.[Code];

    -- Otherwise preserve the most commonly used legacy supplier control account.
    IF @SupplierParentId IS NULL AND OBJECT_ID(N'[dbo].[tbl_SupplierAccounts]', N'U') IS NOT NULL
    BEGIN
        SELECT TOP (1) @SupplierParentId = sa.[ControlAccountId]
        FROM [dbo].[tbl_SupplierAccounts] sa
        INNER JOIN [dbo].[tbl_Accounts] a ON a.[Id] = sa.[ControlAccountId]
        GROUP BY sa.[ControlAccountId]
        ORDER BY COUNT_BIG(*) DESC, sa.[ControlAccountId];
    END;

    -- Last resort: create a dedicated valid control account for migrated historical suppliers.
    IF @SupplierParentId IS NULL
    BEGIN
        SET @SupplierParentId = NEWID();
        DECLARE @LegacySupplierControlCode nvarchar(30) =
            N'AP-LEGACY-' + LEFT(REPLACE(CONVERT(nvarchar(36), @SupplierParentId), N'-', N''), 8);

        INSERT INTO [dbo].[tbl_Accounts]
        (
            [Id],[Code],[NameAr],[NameEn],[ParentAccountId],[Level],[AccountClass],[AccountType],
            [NormalBalance],[IsPostingAccount],[IsControlAccount],[AllowManualPosting],
            [IsSystemAccount],[IsActive],[EffectiveDate],
            [CreatedAt],[CreatedBy],[UpdatedAt],[UpdatedBy],[CreatedFromDevice],[UpdatedFromDevice]
        )
        VALUES
        (
            @SupplierParentId,@LegacySupplierControlCode,N'حساب الموردين المرحّلين',N'Legacy Migrated Suppliers',
            NULL,1,2,3,2,0,1,0,1,1,NULL,
            @MigrationNow,@MigrationActor,NULL,NULL,NULL,NULL
        );
    END;

    DECLARE @SupplierCodeBase bigint = ISNULL
    (
        (SELECT MAX(TRY_CONVERT(bigint, SUBSTRING([SupplierCode], 5, 20)))
         FROM [dbo].[tbl_Suppliers]
         WHERE [SupplierCode] LIKE N'SUP-%'), 0
    );

    DECLARE @ApCodeBase bigint = ISNULL
    (
        (SELECT MAX(TRY_CONVERT(bigint, SUBSTRING([Code], 4, 20)))
         FROM [dbo].[tbl_Accounts]
         WHERE [Code] LIKE N'AP-%'), 0
    );

    DECLARE @OrphanSuppliers TABLE
    (
        [SupplierId] uniqueidentifier NOT NULL PRIMARY KEY,
        [AccountId] uniqueidentifier NOT NULL,
        [SupplierCode] nvarchar(32) NOT NULL,
        [AccountCode] nvarchar(30) NOT NULL,
        [NameAr] nvarchar(150) NOT NULL
    );

    ;WITH refs AS
    (
        SELECT v.[SupplierId], NULLIF(LTRIM(RTRIM(v.[BeneficiaryName])), N'') AS [NameAr]
        FROM [dbo].[tbl_PaymentVouchers] v
        WHERE v.[SupplierId] IS NOT NULL

        UNION ALL

        SELECT l.[SupplierId], NULLIF(LTRIM(RTRIM(l.[Description])), N'') AS [NameAr]
        FROM [dbo].[tbl_JournalEntryLines] l
        WHERE l.[SupplierId] IS NOT NULL
    ), grouped AS
    (
        SELECT r.[SupplierId], MAX(r.[NameAr]) AS [NameAr]
        FROM refs r
        WHERE NOT EXISTS (SELECT 1 FROM [dbo].[tbl_Suppliers] s WHERE s.[Id] = r.[SupplierId])
        GROUP BY r.[SupplierId]
    ), numbered AS
    (
        SELECT g.[SupplierId], g.[NameAr], ROW_NUMBER() OVER (ORDER BY g.[SupplierId]) AS rn
        FROM grouped g
    )
    INSERT INTO @OrphanSuppliers ([SupplierId],[AccountId],[SupplierCode],[AccountCode],[NameAr])
    SELECT
        n.[SupplierId],
        NEWID(),
        N'SUP-' + RIGHT(N'000000' + CONVERT(nvarchar(20), @SupplierCodeBase + n.rn), 6),
        N'AP-'  + RIGHT(N'000000' + CONVERT(nvarchar(20), @ApCodeBase + n.rn), 6),
        LEFT(COALESCE(n.[NameAr], N'مورد مرحّل - ' + CONVERT(nvarchar(36), n.[SupplierId])), 150)
    FROM numbered n;

    INSERT INTO [dbo].[tbl_Accounts]
    (
        [Id],[Code],[NameAr],[NameEn],[ParentAccountId],[Level],[AccountClass],[AccountType],
        [NormalBalance],[IsPostingAccount],[IsControlAccount],[AllowManualPosting],
        [IsSystemAccount],[IsActive],[EffectiveDate],
        [CreatedAt],[CreatedBy],[UpdatedAt],[UpdatedBy],[CreatedFromDevice],[UpdatedFromDevice]
    )
    SELECT
        o.[AccountId],o.[AccountCode],o.[NameAr],NULL,@SupplierParentId,
        CONVERT(tinyint, CASE WHEN p.[Level] < 255 THEN p.[Level] + 1 ELSE 255 END),
        2,4,2,1,0,0,0,0,NULL,
        @MigrationNow,@MigrationActor,NULL,NULL,NULL,NULL
    FROM @OrphanSuppliers o
    INNER JOIN [dbo].[tbl_Accounts] p ON p.[Id] = @SupplierParentId;

    INSERT INTO [dbo].[tbl_Suppliers]
    (
        [Id],[SupplierCode],[AccountId],[EntityType],[SupplierScope],[NameAr],[NameEn],[TradeName],
        [NationalId],[CommercialRegistrationNo],[TaxNumber],[ContactPersonName],[ContactPersonTitle],
        [Phone],[Mobile],[AlternatePhone],[WhatsAppNumber],[Email],[Website],[PreferredContactMethod],
        [Country],[Governorate],[City],[District],[Street],[Building],[PostalCode],[AddressDetails],
        [CreditLimit],[PaymentTermDays],[DefaultLeadTimeDays],[SupplierSince],[IsActive],[Notes],
        [CreatedAt],[CreatedBy],[UpdatedAt],[UpdatedBy],[CreatedFromDevice],[UpdatedFromDevice]
    )
    SELECT
        o.[SupplierId],o.[SupplierCode],o.[AccountId],0,0,o.[NameAr],NULL,NULL,
        NULL,NULL,NULL,NULL,NULL,
        NULL,NULL,NULL,NULL,NULL,NULL,0,
        NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,
        0,0,NULL,NULL,0,N'تم إنشاء هذا السجل تلقائيًا للحفاظ على مرجع محاسبي تاريخي أثناء الترحيل.',
        @MigrationNow,@MigrationActor,NULL,NULL,NULL,NULL
    FROM @OrphanSuppliers o;
END;

-- After backfill, these checks should only fail if the source data changed concurrently
-- or there is a genuinely corrupt reference that could not be represented.
IF EXISTS(SELECT 1 FROM [dbo].[tbl_ReceiptVouchers] v WHERE v.CustomerId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM [dbo].[tbl_Customers] c WHERE c.Id=v.CustomerId)) THROW 51003,'Unresolved orphan ReceiptVoucher.CustomerId found after legacy backfill.',1;
IF EXISTS(SELECT 1 FROM [dbo].[tbl_PaymentVouchers] v WHERE v.SupplierId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM [dbo].[tbl_Suppliers] s WHERE s.Id=v.SupplierId)) THROW 51004,'Unresolved orphan PaymentVoucher.SupplierId found after legacy backfill.',1;
IF EXISTS(SELECT 1 FROM [dbo].[tbl_JournalEntryLines] l WHERE l.CustomerId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM [dbo].[tbl_Customers] c WHERE c.Id=l.CustomerId)) THROW 51005,'Unresolved orphan JournalEntryLine.CustomerId found after legacy backfill.',1;
IF EXISTS(SELECT 1 FROM [dbo].[tbl_JournalEntryLines] l WHERE l.SupplierId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM [dbo].[tbl_Suppliers] s WHERE s.Id=l.SupplierId)) THROW 51006,'Unresolved orphan JournalEntryLine.SupplierId found after legacy backfill.',1;

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'UX_Customers_CustomerCode' AND object_id=OBJECT_ID(N'[dbo].[tbl_Customers]')) CREATE UNIQUE INDEX [UX_Customers_CustomerCode] ON [dbo].[tbl_Customers]([CustomerCode]);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'UX_Customers_AccountId' AND object_id=OBJECT_ID(N'[dbo].[tbl_Customers]')) CREATE UNIQUE INDEX [UX_Customers_AccountId] ON [dbo].[tbl_Customers]([AccountId]);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'UX_Customers_NationalId' AND object_id=OBJECT_ID(N'[dbo].[tbl_Customers]')) CREATE UNIQUE INDEX [UX_Customers_NationalId] ON [dbo].[tbl_Customers]([NationalId]) WHERE [NationalId] IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'UX_Customers_TaxNumber' AND object_id=OBJECT_ID(N'[dbo].[tbl_Customers]')) CREATE UNIQUE INDEX [UX_Customers_TaxNumber] ON [dbo].[tbl_Customers]([TaxNumber]) WHERE [TaxNumber] IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'UX_Customers_CommercialRegistrationNo' AND object_id=OBJECT_ID(N'[dbo].[tbl_Customers]')) CREATE UNIQUE INDEX [UX_Customers_CommercialRegistrationNo] ON [dbo].[tbl_Customers]([CommercialRegistrationNo]) WHERE [CommercialRegistrationNo] IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_Customers_NameAr' AND object_id=OBJECT_ID(N'[dbo].[tbl_Customers]')) CREATE INDEX [IX_Customers_NameAr] ON [dbo].[tbl_Customers]([NameAr]);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_Customers_IsActive' AND object_id=OBJECT_ID(N'[dbo].[tbl_Customers]')) CREATE INDEX [IX_Customers_IsActive] ON [dbo].[tbl_Customers]([IsActive]);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_Customers_EntityType' AND object_id=OBJECT_ID(N'[dbo].[tbl_Customers]')) CREATE INDEX [IX_Customers_EntityType] ON [dbo].[tbl_Customers]([EntityType]);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_Customers_Mobile' AND object_id=OBJECT_ID(N'[dbo].[tbl_Customers]')) CREATE INDEX [IX_Customers_Mobile] ON [dbo].[tbl_Customers]([Mobile]);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'UX_Suppliers_SupplierCode' AND object_id=OBJECT_ID(N'[dbo].[tbl_Suppliers]')) CREATE UNIQUE INDEX [UX_Suppliers_SupplierCode] ON [dbo].[tbl_Suppliers]([SupplierCode]);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'UX_Suppliers_AccountId' AND object_id=OBJECT_ID(N'[dbo].[tbl_Suppliers]')) CREATE UNIQUE INDEX [UX_Suppliers_AccountId] ON [dbo].[tbl_Suppliers]([AccountId]);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'UX_Suppliers_NationalId' AND object_id=OBJECT_ID(N'[dbo].[tbl_Suppliers]')) CREATE UNIQUE INDEX [UX_Suppliers_NationalId] ON [dbo].[tbl_Suppliers]([NationalId]) WHERE [NationalId] IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'UX_Suppliers_TaxNumber' AND object_id=OBJECT_ID(N'[dbo].[tbl_Suppliers]')) CREATE UNIQUE INDEX [UX_Suppliers_TaxNumber] ON [dbo].[tbl_Suppliers]([TaxNumber]) WHERE [TaxNumber] IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'UX_Suppliers_CommercialRegistrationNo' AND object_id=OBJECT_ID(N'[dbo].[tbl_Suppliers]')) CREATE UNIQUE INDEX [UX_Suppliers_CommercialRegistrationNo] ON [dbo].[tbl_Suppliers]([CommercialRegistrationNo]) WHERE [CommercialRegistrationNo] IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_Suppliers_NameAr' AND object_id=OBJECT_ID(N'[dbo].[tbl_Suppliers]')) CREATE INDEX [IX_Suppliers_NameAr] ON [dbo].[tbl_Suppliers]([NameAr]);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_Suppliers_IsActive' AND object_id=OBJECT_ID(N'[dbo].[tbl_Suppliers]')) CREATE INDEX [IX_Suppliers_IsActive] ON [dbo].[tbl_Suppliers]([IsActive]);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_Suppliers_EntityType' AND object_id=OBJECT_ID(N'[dbo].[tbl_Suppliers]')) CREATE INDEX [IX_Suppliers_EntityType] ON [dbo].[tbl_Suppliers]([EntityType]);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_Suppliers_SupplierScope' AND object_id=OBJECT_ID(N'[dbo].[tbl_Suppliers]')) CREATE INDEX [IX_Suppliers_SupplierScope] ON [dbo].[tbl_Suppliers]([SupplierScope]);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_Suppliers_Mobile' AND object_id=OBJECT_ID(N'[dbo].[tbl_Suppliers]')) CREATE INDEX [IX_Suppliers_Mobile] ON [dbo].[tbl_Suppliers]([Mobile]);

IF OBJECT_ID(N'[dbo].[tbl_ReceiptVouchers]',N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_ReceiptVouchers_Customer') ALTER TABLE [dbo].[tbl_ReceiptVouchers] ADD CONSTRAINT [FK_ReceiptVouchers_Customer] FOREIGN KEY([CustomerId]) REFERENCES [dbo].[tbl_Customers]([Id]);
IF OBJECT_ID(N'[dbo].[tbl_PaymentVouchers]',N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_PaymentVouchers_Supplier') ALTER TABLE [dbo].[tbl_PaymentVouchers] ADD CONSTRAINT [FK_PaymentVouchers_Supplier] FOREIGN KEY([SupplierId]) REFERENCES [dbo].[tbl_Suppliers]([Id]);
IF OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]',N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_JournalEntryLines_Customer') ALTER TABLE [dbo].[tbl_JournalEntryLines] ADD CONSTRAINT [FK_JournalEntryLines_Customer] FOREIGN KEY([CustomerId]) REFERENCES [dbo].[tbl_Customers]([Id]);
IF OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]',N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_JournalEntryLines_Supplier') ALTER TABLE [dbo].[tbl_JournalEntryLines] ADD CONSTRAINT [FK_JournalEntryLines_Supplier] FOREIGN KEY([SupplierId]) REFERENCES [dbo].[tbl_Suppliers]([Id]);
IF OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]',N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_JournalEntryLines_CustomerId' AND object_id=OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]')) CREATE INDEX [IX_JournalEntryLines_CustomerId] ON [dbo].[tbl_JournalEntryLines]([CustomerId]);
IF OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]',N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_JournalEntryLines_SupplierId' AND object_id=OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]')) CREATE INDEX [IX_JournalEntryLines_SupplierId] ON [dbo].[tbl_JournalEntryLines]([SupplierId]);

DECLARE @CustomerStart bigint = ISNULL((SELECT MAX(TRY_CONVERT(bigint, SUBSTRING(CustomerCode, 5, 20))) FROM [dbo].[tbl_Customers] WHERE CustomerCode LIKE N'CUS-%'), 0) + 1;
DECLARE @SupplierStart bigint = ISNULL((SELECT MAX(TRY_CONVERT(bigint, SUBSTRING(SupplierCode, 5, 20))) FROM [dbo].[tbl_Suppliers] WHERE SupplierCode LIKE N'SUP-%'), 0) + 1;
DECLARE @ArStart bigint = ISNULL((SELECT MAX(TRY_CONVERT(bigint, SUBSTRING(Code, 4, 20))) FROM [dbo].[tbl_Accounts] WHERE Code LIKE N'AR-%'), 0) + 1;
DECLARE @ApStart bigint = ISNULL((SELECT MAX(TRY_CONVERT(bigint, SUBSTRING(Code, 4, 20))) FROM [dbo].[tbl_Accounts] WHERE Code LIKE N'AP-%'), 0) + 1;
DECLARE @SequenceSql nvarchar(max);

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'CustomerCodeSequence' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    SET @SequenceSql = N'CREATE SEQUENCE [dbo].[CustomerCodeSequence] AS bigint START WITH ' + CONVERT(nvarchar(30), @CustomerStart) + N' INCREMENT BY 1 NO CYCLE;';
    EXEC sys.sp_executesql @SequenceSql;
END
ELSE
BEGIN
    SET @SequenceSql = N'ALTER SEQUENCE [dbo].[CustomerCodeSequence] RESTART WITH ' + CONVERT(nvarchar(30), @CustomerStart) + N';';
    EXEC sys.sp_executesql @SequenceSql;
END;

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'SupplierCodeSequence' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    SET @SequenceSql = N'CREATE SEQUENCE [dbo].[SupplierCodeSequence] AS bigint START WITH ' + CONVERT(nvarchar(30), @SupplierStart) + N' INCREMENT BY 1 NO CYCLE;';
    EXEC sys.sp_executesql @SequenceSql;
END
ELSE
BEGIN
    SET @SequenceSql = N'ALTER SEQUENCE [dbo].[SupplierCodeSequence] RESTART WITH ' + CONVERT(nvarchar(30), @SupplierStart) + N';';
    EXEC sys.sp_executesql @SequenceSql;
END;

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'CustomerAccountCodeSequence' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    SET @SequenceSql = N'CREATE SEQUENCE [dbo].[CustomerAccountCodeSequence] AS bigint START WITH ' + CONVERT(nvarchar(30), @ArStart) + N' INCREMENT BY 1 NO CYCLE;';
    EXEC sys.sp_executesql @SequenceSql;
END
ELSE
BEGIN
    SET @SequenceSql = N'ALTER SEQUENCE [dbo].[CustomerAccountCodeSequence] RESTART WITH ' + CONVERT(nvarchar(30), @ArStart) + N';';
    EXEC sys.sp_executesql @SequenceSql;
END;

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'SupplierAccountCodeSequence' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    SET @SequenceSql = N'CREATE SEQUENCE [dbo].[SupplierAccountCodeSequence] AS bigint START WITH ' + CONVERT(nvarchar(30), @ApStart) + N' INCREMENT BY 1 NO CYCLE;';
    EXEC sys.sp_executesql @SequenceSql;
END
ELSE
BEGIN
    SET @SequenceSql = N'ALTER SEQUENCE [dbo].[SupplierAccountCodeSequence] RESTART WITH ' + CONVERT(nvarchar(30), @ApStart) + N';';
    EXEC sys.sp_executesql @SequenceSql;
END;

IF OBJECT_ID(N'[dbo].[tbl_CustomerAccounts]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_CustomerAccounts];
IF OBJECT_ID(N'[dbo].[tbl_SupplierAccounts]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_SupplierAccounts];
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_CustomerAccounts]',N'U') IS NULL
BEGIN
 CREATE TABLE [dbo].[tbl_CustomerAccounts]([Id] uniqueidentifier NOT NULL,[CustomerId] uniqueidentifier NOT NULL,[AccountId] uniqueidentifier NOT NULL,[ControlAccountId] uniqueidentifier NOT NULL,[IsActive] bit NOT NULL,[CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_CustomerAccounts] PRIMARY KEY([Id]),CONSTRAINT [FK_CustomerAccounts_Account] FOREIGN KEY([AccountId]) REFERENCES [dbo].[tbl_Accounts]([Id]),CONSTRAINT [FK_CustomerAccounts_ControlAccount] FOREIGN KEY([ControlAccountId]) REFERENCES [dbo].[tbl_Accounts]([Id]));
 INSERT INTO [dbo].[tbl_CustomerAccounts]([Id],[CustomerId],[AccountId],[ControlAccountId],[IsActive],[CreatedAt],[CreatedBy],[UpdatedAt],[UpdatedBy],[CreatedFromDevice],[UpdatedFromDevice]) SELECT NEWID(),c.Id,c.AccountId,a.ParentAccountId,c.IsActive,c.CreatedAt,c.CreatedBy,c.UpdatedAt,c.UpdatedBy,c.CreatedFromDevice,c.UpdatedFromDevice FROM [dbo].[tbl_Customers] c INNER JOIN [dbo].[tbl_Accounts] a ON a.Id=c.AccountId WHERE a.ParentAccountId IS NOT NULL;
 CREATE UNIQUE INDEX [UX_CustomerAccounts_CustomerId] ON [dbo].[tbl_CustomerAccounts]([CustomerId]);
END;
IF OBJECT_ID(N'[dbo].[tbl_SupplierAccounts]',N'U') IS NULL
BEGIN
 CREATE TABLE [dbo].[tbl_SupplierAccounts]([Id] uniqueidentifier NOT NULL,[SupplierId] uniqueidentifier NOT NULL,[AccountId] uniqueidentifier NOT NULL,[ControlAccountId] uniqueidentifier NOT NULL,[IsActive] bit NOT NULL,[CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_SupplierAccounts] PRIMARY KEY([Id]),CONSTRAINT [FK_SupplierAccounts_Account] FOREIGN KEY([AccountId]) REFERENCES [dbo].[tbl_Accounts]([Id]),CONSTRAINT [FK_SupplierAccounts_ControlAccount] FOREIGN KEY([ControlAccountId]) REFERENCES [dbo].[tbl_Accounts]([Id]));
 INSERT INTO [dbo].[tbl_SupplierAccounts]([Id],[SupplierId],[AccountId],[ControlAccountId],[IsActive],[CreatedAt],[CreatedBy],[UpdatedAt],[UpdatedBy],[CreatedFromDevice],[UpdatedFromDevice]) SELECT NEWID(),s.Id,s.AccountId,a.ParentAccountId,s.IsActive,s.CreatedAt,s.CreatedBy,s.UpdatedAt,s.UpdatedBy,s.CreatedFromDevice,s.UpdatedFromDevice FROM [dbo].[tbl_Suppliers] s INNER JOIN [dbo].[tbl_Accounts] a ON a.Id=s.AccountId WHERE a.ParentAccountId IS NOT NULL;
 CREATE UNIQUE INDEX [UX_SupplierAccounts_SupplierId] ON [dbo].[tbl_SupplierAccounts]([SupplierId]);
END;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_ReceiptVouchers_Customer') ALTER TABLE [dbo].[tbl_ReceiptVouchers] DROP CONSTRAINT [FK_ReceiptVouchers_Customer];
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_PaymentVouchers_Supplier') ALTER TABLE [dbo].[tbl_PaymentVouchers] DROP CONSTRAINT [FK_PaymentVouchers_Supplier];
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_JournalEntryLines_Customer') ALTER TABLE [dbo].[tbl_JournalEntryLines] DROP CONSTRAINT [FK_JournalEntryLines_Customer];
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_JournalEntryLines_Supplier') ALTER TABLE [dbo].[tbl_JournalEntryLines] DROP CONSTRAINT [FK_JournalEntryLines_Supplier];
IF EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_JournalEntryLines_CustomerId' AND object_id=OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]')) DROP INDEX [IX_JournalEntryLines_CustomerId] ON [dbo].[tbl_JournalEntryLines];
IF EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_JournalEntryLines_SupplierId' AND object_id=OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]')) DROP INDEX [IX_JournalEntryLines_SupplierId] ON [dbo].[tbl_JournalEntryLines];
IF OBJECT_ID(N'[dbo].[tbl_Customers]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_Customers];
IF OBJECT_ID(N'[dbo].[tbl_Suppliers]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_Suppliers];
IF EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'CustomerCodeSequence' AND schema_id=SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[CustomerCodeSequence];
IF EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'SupplierCodeSequence' AND schema_id=SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[SupplierCodeSequence];
IF EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'CustomerAccountCodeSequence' AND schema_id=SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[CustomerAccountCodeSequence];
IF EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'SupplierAccountCodeSequence' AND schema_id=SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[SupplierAccountCodeSequence];
""");
    }
}
