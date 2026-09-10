using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260910170000_EmployeeContactInfo")]
public sealed class EmployeeContactInfo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Email",
            schema: "hr",
            table: "Employees",
            type: "nvarchar(256)",
            maxLength: 256,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Country",
            schema: "hr",
            table: "Employees",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Governorate",
            schema: "hr",
            table: "Employees",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "City",
            schema: "hr",
            table: "Employees",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PostalCode",
            schema: "hr",
            table: "Employees",
            type: "nvarchar(24)",
            maxLength: 24,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ResidentialAddress",
            schema: "hr",
            table: "Employees",
            type: "nvarchar(300)",
            maxLength: 300,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Employees_Email",
            schema: "hr",
            table: "Employees",
            column: "Email",
            filter: "[Email] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Employees_Email",
            schema: "hr",
            table: "Employees");

        migrationBuilder.DropColumn(name: "Email", schema: "hr", table: "Employees");
        migrationBuilder.DropColumn(name: "Country", schema: "hr", table: "Employees");
        migrationBuilder.DropColumn(name: "Governorate", schema: "hr", table: "Employees");
        migrationBuilder.DropColumn(name: "City", schema: "hr", table: "Employees");
        migrationBuilder.DropColumn(name: "PostalCode", schema: "hr", table: "Employees");
        migrationBuilder.DropColumn(name: "ResidentialAddress", schema: "hr", table: "Employees");
    }
}
