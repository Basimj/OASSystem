using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260909203000_AddEmployeeImages")]
public sealed class AddEmployeeImages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[hr].[EmployeeImages]', N'U') IS NULL
BEGIN
    CREATE TABLE [hr].[EmployeeImages](
        [EmployeeId] uniqueidentifier NOT NULL,
        [ContentType] nvarchar(64) NOT NULL,
        [ImageData] varbinary(max) NOT NULL,
        [FileSize] int NOT NULL,
        [UpdatedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_EmployeeImages] PRIMARY KEY ([EmployeeId]),
        CONSTRAINT [FK_EmployeeImages_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [hr].[Employees]([Id]) ON DELETE CASCADE,
        CONSTRAINT [CK_EmployeeImages_FileSize] CHECK ([FileSize] > 0 AND [FileSize] <= 2500000)
    );
END
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[hr].[EmployeeImages]', N'U') IS NOT NULL
    DROP TABLE [hr].[EmployeeImages];
""");
    }
}
