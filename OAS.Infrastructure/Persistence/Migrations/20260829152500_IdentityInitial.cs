using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260829152500_IdentityInitial")]
public sealed class IdentityInitial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF SCHEMA_ID(N'security') IS NULL EXEC(N'CREATE SCHEMA [security]');
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[Roles]', N'U') IS NULL
BEGIN
    CREATE TABLE [security].[Roles](
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Roles] PRIMARY KEY,
        [Name] nvarchar(64) NOT NULL,
        [NormalizedName] nvarchar(64) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        [IsSystem] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [LastModifiedAtUtc] datetimeoffset NULL,
        [LastModifiedBy] nvarchar(64) NULL
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[security].[Roles]') AND name = N'IX_Roles_NormalizedName')
    CREATE UNIQUE INDEX [IX_Roles_NormalizedName] ON [security].[Roles]([NormalizedName]);
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[Users]', N'U') IS NULL
BEGIN
    CREATE TABLE [security].[Users](
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Users] PRIMARY KEY,
        [UserName] nvarchar(64) NOT NULL,
        [NormalizedUserName] nvarchar(64) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [IsActive] bit NOT NULL,
        [IsSuperAdmin] bit NOT NULL CONSTRAINT [DF_Users_IsSuperAdmin] DEFAULT(0),
        [MustChangePassword] bit NOT NULL CONSTRAINT [DF_Users_MustChangePassword] DEFAULT(0),
        [AccessFailedCount] int NOT NULL,
        [LockoutEndUtc] datetimeoffset NULL,
        [LastLoginAtUtc] datetimeoffset NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [LastModifiedAtUtc] datetimeoffset NULL,
        [LastModifiedBy] nvarchar(64) NULL
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[security].[Users]') AND name = N'IX_Users_NormalizedUserName')
    CREATE UNIQUE INDEX [IX_Users_NormalizedUserName] ON [security].[Users]([NormalizedUserName]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[security].[Users]') AND name = N'IX_Users_NormalizedEmail')
    CREATE UNIQUE INDEX [IX_Users_NormalizedEmail] ON [security].[Users]([NormalizedEmail]) WHERE [NormalizedEmail] IS NOT NULL;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[UserRoles]', N'U') IS NULL
BEGIN
    CREATE TABLE [security].[UserRoles](
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_UserRoles] PRIMARY KEY,
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY([UserId]) REFERENCES [security].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY([RoleId]) REFERENCES [security].[Roles]([Id]) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[security].[UserRoles]') AND name = N'IX_UserRoles_UserId_RoleId')
    CREATE UNIQUE INDEX [IX_UserRoles_UserId_RoleId] ON [security].[UserRoles]([UserId],[RoleId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[security].[UserRoles]') AND name = N'IX_UserRoles_RoleId')
    CREATE INDEX [IX_UserRoles_RoleId] ON [security].[UserRoles]([RoleId]);
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[UserPasswordHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [security].[UserPasswordHistory](
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_UserPasswordHistory] PRIMARY KEY,
        [UserId] uniqueidentifier NOT NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [FK_UserPasswordHistory_Users_UserId] FOREIGN KEY([UserId]) REFERENCES [security].[Users]([Id]) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[security].[UserPasswordHistory]') AND name = N'IX_UserPasswordHistory_UserId_CreatedAtUtc')
    CREATE INDEX [IX_UserPasswordHistory_UserId_CreatedAtUtc] ON [security].[UserPasswordHistory]([UserId],[CreatedAtUtc]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM [security].[Roles] WHERE [Id] = '8F4A57C8-40A6-45B7-8C7B-C2264FA07001')
    INSERT INTO [security].[Roles]([Id],[Name],[NormalizedName],[DisplayName],[IsSystem],[CreatedAtUtc]) VALUES('8F4A57C8-40A6-45B7-8C7B-C2264FA07001',N'Administrator',N'ADMINISTRATOR',N'Administrator',1,SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM [security].[Roles] WHERE [Id] = '8F4A57C8-40A6-45B7-8C7B-C2264FA07002')
    INSERT INTO [security].[Roles]([Id],[Name],[NormalizedName],[DisplayName],[IsSystem],[CreatedAtUtc]) VALUES('8F4A57C8-40A6-45B7-8C7B-C2264FA07002',N'User',N'USER',N'User',1,SYSUTCDATETIME());
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM [security].[Users] WHERE [NormalizedUserName] = N'ADMIN@GMAIL.COM')
BEGIN
    INSERT INTO [security].[Users]([Id],[UserName],[NormalizedUserName],[FirstName],[LastName],[Email],[NormalizedEmail],[PasswordHash],[IsActive],[IsSuperAdmin],[MustChangePassword],[AccessFailedCount],[CreatedAtUtc])
    VALUES('8F4A57C8-40A6-45B7-8C7B-C2264FA07999',N'admin@gmail.com',N'ADMIN@GMAIL.COM',N'System',N'Administrator',N'admin@gmail.com',N'ADMIN@GMAIL.COM',N'AQAAAAIAAYagAAAAEHLzOTXtVk1vB7amFQVEe59/JMDd7Z6T14I0YETzFkwRt+BdFW3rwWjNg3Kzr3HYHg==',1,1,1,0,SYSUTCDATETIME());
END;
""");

        migrationBuilder.Sql("""
DECLARE @AdminId uniqueidentifier = (SELECT TOP(1) [Id] FROM [security].[Users] WHERE [NormalizedUserName] = N'ADMIN@GMAIL.COM');
IF @AdminId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [security].[UserRoles] WHERE [UserId] = @AdminId AND [RoleId] = '8F4A57C8-40A6-45B7-8C7B-C2264FA07001')
    INSERT INTO [security].[UserRoles]([Id],[UserId],[RoleId]) VALUES('8F4A57C8-40A6-45B7-8C7B-C2264FA07801',@AdminId,'8F4A57C8-40A6-45B7-8C7B-C2264FA07001');
IF @AdminId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [security].[UserPasswordHistory] WHERE [UserId] = @AdminId)
    INSERT INTO [security].[UserPasswordHistory]([Id],[UserId],[PasswordHash],[CreatedAtUtc]) SELECT '8F4A57C8-40A6-45B7-8C7B-C2264FA07802',@AdminId,[PasswordHash],SYSUTCDATETIME() FROM [security].[Users] WHERE [Id] = @AdminId;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[UserPasswordHistory]', N'U') IS NOT NULL DROP TABLE [security].[UserPasswordHistory];
IF OBJECT_ID(N'[security].[UserRoles]', N'U') IS NOT NULL DROP TABLE [security].[UserRoles];
IF OBJECT_ID(N'[security].[Users]', N'U') IS NOT NULL DROP TABLE [security].[Users];
IF OBJECT_ID(N'[security].[Roles]', N'U') IS NOT NULL DROP TABLE [security].[Roles];
""");
    }
}
