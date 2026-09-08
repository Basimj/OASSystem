using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260908180000_IdentityUsersWorkspaceBootstrap")]
public sealed class IdentityUsersWorkspaceBootstrap : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[Roles]', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM [security].[Roles] WHERE [Id] = '8F4A57C8-40A6-45B7-8C7B-C2264FA07001')
BEGIN
    INSERT INTO [security].[Roles]
        ([Id],[Name],[NormalizedName],[DisplayName],[IsSystem],[CreatedAtUtc])
    VALUES
        ('8F4A57C8-40A6-45B7-8C7B-C2264FA07001',N'Administrator',N'ADMINISTRATOR',N'Administrator',1,SYSUTCDATETIME());
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[Users]', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM [security].[Users] WHERE [Id] = '8F4A57C8-40A6-45B7-8C7B-C2264FA07999')
AND NOT EXISTS (SELECT 1 FROM [security].[Users] WHERE [NormalizedUserName] = N'ADMIN@GMAIL.COM')
BEGIN
    INSERT INTO [security].[Users]
        ([Id],[UserName],[NormalizedUserName],[FirstName],[LastName],[Email],[NormalizedEmail],[PasswordHash],
         [IsActive],[IsSuperAdmin],[MustChangePassword],[AccessFailedCount],[CreatedAtUtc])
    VALUES
        ('8F4A57C8-40A6-45B7-8C7B-C2264FA07999',N'admin@gmail.com',N'ADMIN@GMAIL.COM',N'System',N'Administrator',
         N'admin@gmail.com',N'ADMIN@GMAIL.COM',N'OAS_BOOTSTRAP_NO_PASSWORD_V1',1,1,1,0,SYSUTCDATETIME());
END;
""");

        // Safely convert only the untouched legacy bootstrap credential. A configured Super Admin is never reset.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[Users]', N'U') IS NOT NULL
BEGIN
    DECLARE @LegacyBootstrapHash nvarchar(512) = N'AQAAAAIAAYagAAAAEHLzOTXtVk1vB7amFQVEe59/JMDd7Z6T14I0YETzFkwRt+BdFW3rwWjNg3Kzr3HYHg==';

    IF EXISTS
    (
        SELECT 1
        FROM [security].[Users]
        WHERE [Id] = '8F4A57C8-40A6-45B7-8C7B-C2264FA07999'
          AND [PasswordHash] = @LegacyBootstrapHash
          AND [IsSuperAdmin] = 1
          AND [MustChangePassword] = 1
          AND [LastLoginAtUtc] IS NULL
    )
    BEGIN
        UPDATE [security].[Users]
        SET [PasswordHash] = N'OAS_BOOTSTRAP_NO_PASSWORD_V1',
            [AccessFailedCount] = 0,
            [LockoutEndUtc] = NULL
        WHERE [Id] = '8F4A57C8-40A6-45B7-8C7B-C2264FA07999';

        IF OBJECT_ID(N'[security].[UserPasswordHistory]', N'U') IS NOT NULL
            DELETE FROM [security].[UserPasswordHistory]
            WHERE [UserId] = '8F4A57C8-40A6-45B7-8C7B-C2264FA07999'
              AND [PasswordHash] = @LegacyBootstrapHash;
    END;
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[security].[Users]', N'U') IS NOT NULL
AND OBJECT_ID(N'[security].[UserRoles]', N'U') IS NOT NULL
AND EXISTS (SELECT 1 FROM [security].[Users] WHERE [Id] = '8F4A57C8-40A6-45B7-8C7B-C2264FA07999')
AND NOT EXISTS
(
    SELECT 1 FROM [security].[UserRoles]
    WHERE [UserId] = '8F4A57C8-40A6-45B7-8C7B-C2264FA07999'
      AND [RoleId] = '8F4A57C8-40A6-45B7-8C7B-C2264FA07001'
)
BEGIN
    INSERT INTO [security].[UserRoles]([Id],[UserId],[RoleId])
    VALUES(NEWID(),'8F4A57C8-40A6-45B7-8C7B-C2264FA07999','8F4A57C8-40A6-45B7-8C7B-C2264FA07001');
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data bootstrap is intentionally non-destructive.
    }
}
