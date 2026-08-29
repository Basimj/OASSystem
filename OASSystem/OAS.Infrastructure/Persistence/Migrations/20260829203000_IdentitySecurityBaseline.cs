using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260829203000_IdentitySecurityBaseline")]
public sealed class IdentitySecurityBaseline : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF SCHEMA_ID(N'security') IS NULL EXEC(N'CREATE SCHEMA [security]');
""");

        // Schema changes are deliberately isolated from later DML commands.
        // SQL Server compiles each batch before execution; using a newly-added
        // column in the same batch can otherwise produce "Invalid column name".
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[Users]', N'U') IS NOT NULL AND COL_LENGTH(N'security.Users', N'IsSuperAdmin') IS NULL
    ALTER TABLE [security].[Users] ADD [IsSuperAdmin] bit NOT NULL CONSTRAINT [DF_Users_IsSuperAdmin] DEFAULT(0);
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[Users]', N'U') IS NOT NULL AND COL_LENGTH(N'security.Users', N'MustChangePassword') IS NULL
    ALTER TABLE [security].[Users] ADD [MustChangePassword] bit NOT NULL CONSTRAINT [DF_Users_MustChangePassword] DEFAULT(0);
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[UserPasswordHistory]', N'U') IS NULL AND OBJECT_ID(N'[security].[Users]', N'U') IS NOT NULL
BEGIN
    CREATE TABLE [security].[UserPasswordHistory](
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_UserPasswordHistory] PRIMARY KEY,
        [UserId] uniqueidentifier NOT NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [FK_UserPasswordHistory_Users_UserId] FOREIGN KEY([UserId]) REFERENCES [security].[Users]([Id]) ON DELETE CASCADE
    );
END;
IF OBJECT_ID(N'[security].[UserPasswordHistory]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[security].[UserPasswordHistory]') AND name = N'IX_UserPasswordHistory_UserId_CreatedAtUtc')
    CREATE INDEX [IX_UserPasswordHistory_UserId_CreatedAtUtc] ON [security].[UserPasswordHistory]([UserId],[CreatedAtUtc]);
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[Roles]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [security].[Roles] WHERE [Id] = '8F4A57C8-40A6-45B7-8C7B-C2264FA07001')
        INSERT INTO [security].[Roles]([Id],[Name],[NormalizedName],[DisplayName],[IsSystem],[CreatedAtUtc]) VALUES('8F4A57C8-40A6-45B7-8C7B-C2264FA07001',N'Administrator',N'ADMINISTRATOR',N'Administrator',1,SYSUTCDATETIME());
    IF NOT EXISTS (SELECT 1 FROM [security].[Roles] WHERE [Id] = '8F4A57C8-40A6-45B7-8C7B-C2264FA07002')
        INSERT INTO [security].[Roles]([Id],[Name],[NormalizedName],[DisplayName],[IsSystem],[CreatedAtUtc]) VALUES('8F4A57C8-40A6-45B7-8C7B-C2264FA07002',N'User',N'USER',N'User',1,SYSUTCDATETIME());
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[Users]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [security].[Users] WHERE [NormalizedUserName] = N'ADMIN@GMAIL.COM')
BEGIN
    INSERT INTO [security].[Users]([Id],[UserName],[NormalizedUserName],[FirstName],[LastName],[Email],[NormalizedEmail],[PasswordHash],[IsActive],[IsSuperAdmin],[MustChangePassword],[AccessFailedCount],[CreatedAtUtc])
    VALUES('8F4A57C8-40A6-45B7-8C7B-C2264FA07999',N'admin@gmail.com',N'ADMIN@GMAIL.COM',N'System',N'Administrator',N'admin@gmail.com',N'ADMIN@GMAIL.COM',N'AQAAAAIAAYagAAAAEHLzOTXtVk1vB7amFQVEe59/JMDd7Z6T14I0YETzFkwRt+BdFW3rwWjNg3Kzr3HYHg==',1,1,1,0,SYSUTCDATETIME());
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[Users]', N'U') IS NOT NULL
BEGIN
    DECLARE @AdminId uniqueidentifier = (SELECT TOP(1) [Id] FROM [security].[Users] WHERE [NormalizedUserName] = N'ADMIN@GMAIL.COM');
    IF @AdminId IS NOT NULL
        UPDATE [security].[Users] SET [IsSuperAdmin] = 1, [MustChangePassword] = 1 WHERE [Id] = @AdminId;
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[Users]', N'U') IS NOT NULL AND OBJECT_ID(N'[security].[UserRoles]', N'U') IS NOT NULL
BEGIN
    DECLARE @AdminId uniqueidentifier = (SELECT TOP(1) [Id] FROM [security].[Users] WHERE [NormalizedUserName] = N'ADMIN@GMAIL.COM');
    IF @AdminId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [security].[UserRoles] WHERE [UserId] = @AdminId AND [RoleId] = '8F4A57C8-40A6-45B7-8C7B-C2264FA07001')
        INSERT INTO [security].[UserRoles]([Id],[UserId],[RoleId]) VALUES('8F4A57C8-40A6-45B7-8C7B-C2264FA07801',@AdminId,'8F4A57C8-40A6-45B7-8C7B-C2264FA07001');
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[Users]', N'U') IS NOT NULL AND OBJECT_ID(N'[security].[UserPasswordHistory]', N'U') IS NOT NULL
BEGIN
    DECLARE @AdminId uniqueidentifier = (SELECT TOP(1) [Id] FROM [security].[Users] WHERE [NormalizedUserName] = N'ADMIN@GMAIL.COM');
    IF @AdminId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [security].[UserPasswordHistory] WHERE [UserId] = @AdminId)
        INSERT INTO [security].[UserPasswordHistory]([Id],[UserId],[PasswordHash],[CreatedAtUtc])
        SELECT '8F4A57C8-40A6-45B7-8C7B-C2264FA07802',@AdminId,[PasswordHash],SYSUTCDATETIME()
        FROM [security].[Users]
        WHERE [Id] = @AdminId;
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Baseline migration is intentionally non-destructive.
    }
}
