using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260906060000_AddEmployeeNumberSequence")]
public sealed class AddEmployeeNumberSequence : Migration
{
    protected override void Up(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF SCHEMA_ID(N'core') IS NULL
    EXEC(N'CREATE SCHEMA [core]');
""");

        migrationBuilder.Sql("""
IF NOT EXISTS
(
    SELECT 1
    FROM sys.sequences
    WHERE name = N'EmployeeNumberSequence'
      AND schema_id = SCHEMA_ID(N'core')
)
BEGIN
    EXEC(N'
        CREATE SEQUENCE [core].[EmployeeNumberSequence]
        AS int
        START WITH 1
        INCREMENT BY 1
        MINVALUE 1
        NO CYCLE;
    ');
END;
""");
    }

    protected override void Down(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF EXISTS
(
    SELECT 1
    FROM sys.sequences
    WHERE name = N'EmployeeNumberSequence'
      AND schema_id = SCHEMA_ID(N'core')
)
BEGIN
    DROP SEQUENCE [core].[EmployeeNumberSequence];
END;
""");
    }
}