using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260909170000_IdentityProfileAndRolesEnhancements")]
public sealed class IdentityProfileAndRolesEnhancements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF COL_LENGTH(N'[security].[Users]', N'PhoneNumber') IS NULL
    ALTER TABLE [security].[Users] ADD [PhoneNumber] nvarchar(32) NULL;

-- Repair databases where the migration history was updated but the profile-image table was not physically created.
IF OBJECT_ID(N'[security].[UserProfileImages]', N'U') IS NULL
BEGIN
    CREATE TABLE [security].[UserProfileImages](
        [UserId] uniqueidentifier NOT NULL,
        [ContentType] nvarchar(64) NOT NULL,
        [ImageData] varbinary(max) NOT NULL,
        [FileSize] int NOT NULL,
        [UpdatedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_UserProfileImages] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_UserProfileImages_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [security].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [CK_UserProfileImages_FileSize] CHECK ([FileSize] > 0 AND [FileSize] <= 2500000)
    );
END
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF COL_LENGTH(N'[security].[Users]', N'PhoneNumber') IS NOT NULL
    ALTER TABLE [security].[Users] DROP COLUMN [PhoneNumber];
""");
    }
}
